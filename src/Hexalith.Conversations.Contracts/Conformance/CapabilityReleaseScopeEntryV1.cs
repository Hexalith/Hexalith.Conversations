// <copyright file="CapabilityReleaseScopeEntryV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Carries all required governance fields for one capability release scope classification entry (FR100, FR101).
/// </summary>
/// <param name="CapabilityId">The stable bounded machine-readable capability identifier.</param>
/// <param name="Scope">The release scope classification for this capability.</param>
/// <param name="ConsequenceAreas">The substrate consequence areas affected when capability is deferred; empty is allowed at construction but validated by <see cref="CapabilityReleaseScopeValidator"/>.</param>
/// <param name="RequirementRef">The optional bounded requirement reference token.</param>
/// <param name="ReleaseGateRef">The optional bounded release gate reference token.</param>
/// <param name="DependencyRef">The optional bounded dependency reference token.</param>
/// <param name="Owner">The bounded owner identifier.</param>
/// <param name="ReviewDateUtc">The UTC review date.</param>
/// <param name="WaiverRef">The optional bounded waiver reference; null allowed at construction but validated by <see cref="CapabilityReleaseScopeValidator"/> for waived scope.</param>
/// <param name="ConditionalExpiry">The optional UTC expiry date for conditional scope; validated by <see cref="CapabilityReleaseScopeValidator"/>.</param>
public sealed record CapabilityReleaseScopeEntryV1(
    string CapabilityId,
    CapabilityReleaseScope Scope,
    IReadOnlyList<SubstrateConsequenceArea> ConsequenceAreas,
    string? RequirementRef,
    string? ReleaseGateRef,
    string? DependencyRef,
    string Owner,
    DateTimeOffset ReviewDateUtc,
    string? WaiverRef,
    DateTimeOffset? ConditionalExpiry)
{
    /// <summary>Gets the stable bounded machine-readable capability identifier.</summary>
    public string CapabilityId { get; } = ConformanceContractValidation.RequiredSafeToken(CapabilityId, nameof(CapabilityId));

    /// <summary>Gets the release scope classification.</summary>
    public CapabilityReleaseScope Scope { get; } = Scope ?? throw new ArgumentNullException(nameof(Scope));

    /// <summary>Gets the substrate consequence areas affected when capability is deferred.</summary>
    public IReadOnlyList<SubstrateConsequenceArea> ConsequenceAreas { get; } = ConsequenceAreas ?? throw new ArgumentNullException(nameof(ConsequenceAreas));

    /// <summary>Gets the optional bounded requirement reference token.</summary>
    public string? RequirementRef { get; } = ConformanceContractValidation.OptionalSafeToken(RequirementRef, nameof(RequirementRef));

    /// <summary>Gets the optional bounded release gate reference token.</summary>
    public string? ReleaseGateRef { get; } = ConformanceContractValidation.OptionalSafeToken(ReleaseGateRef, nameof(ReleaseGateRef));

    /// <summary>Gets the optional bounded dependency reference token.</summary>
    public string? DependencyRef { get; } = ConformanceContractValidation.OptionalSafeToken(DependencyRef, nameof(DependencyRef));

    /// <summary>Gets the bounded owner identifier.</summary>
    public string Owner { get; } = ConformanceContractValidation.RequiredSafeToken(Owner, nameof(Owner));

    /// <summary>Gets the UTC review date.</summary>
    public DateTimeOffset ReviewDateUtc { get; } = ConformanceContractValidation.RequiredUtcTimestamp(ReviewDateUtc, nameof(ReviewDateUtc));

    /// <summary>Gets the optional bounded waiver reference.</summary>
    public string? WaiverRef { get; } = ConformanceContractValidation.OptionalSafeToken(WaiverRef, nameof(WaiverRef));

    /// <summary>Gets the optional UTC expiry date for conditional scope.</summary>
    public DateTimeOffset? ConditionalExpiry { get; } = ConditionalExpiry.HasValue
        ? ConformanceContractValidation.RequiredUtcTimestamp(ConditionalExpiry.Value, nameof(ConditionalExpiry))
        : (DateTimeOffset?)null;
}

/// <summary>
/// Validates a <see cref="CapabilityReleaseScopeEntryV1"/> and returns content-safe typed error tokens (FR100, FR101).
/// </summary>
public static class CapabilityReleaseScopeValidator
{
    /// <summary>
    /// Validates the entry and returns typed diagnostic error tokens for any scope violations.
    /// </summary>
    /// <param name="entry">The entry to validate.</param>
    /// <param name="evaluatedAt">The point-in-time at which to evaluate temporal conditions.</param>
    /// <returns>A read-only list of content-safe error tokens; empty when the entry is valid.</returns>
    public static IReadOnlyList<string> ValidateEntry(CapabilityReleaseScopeEntryV1 entry, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(entry);

        List<string> errors = [];

        if (entry.Scope.Equals(CapabilityReleaseScope.Deferred) && entry.ConsequenceAreas.Count == 0)
        {
            errors.Add("deferred-substrate-no-consequences");
        }

        if (entry.Scope.Equals(CapabilityReleaseScope.Waived) && entry.WaiverRef is null)
        {
            errors.Add("waived-no-reference");
        }

        if (entry.Scope.Equals(CapabilityReleaseScope.Conditional) &&
            (!entry.ConditionalExpiry.HasValue || entry.ConditionalExpiry.Value < evaluatedAt))
        {
            errors.Add("expired-conditional-scope");
        }

        return errors;
    }
}
