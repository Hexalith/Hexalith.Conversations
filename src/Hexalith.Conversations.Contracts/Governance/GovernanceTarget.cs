// <copyright file="GovernanceTarget.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Identifies the governed target without carrying governed content or Party personal data.
/// </summary>
/// <param name="kind">The target category.</param>
/// <param name="messageId">The optional message reference.</param>
/// <param name="fileId">The optional file reference.</param>
/// <param name="partyId">The optional participant Party reference.</param>
/// <param name="segmentReference">An optional opaque content segment reference.</param>
public sealed record GovernanceTarget(
    GovernedTargetKind Kind,
    MessageId? MessageId = null,
    FileId? FileId = null,
    PartyId? PartyId = null,
    string? SegmentReference = null)
{
    public GovernedTargetKind Kind { get; } = GovernanceContractValidation.RequireNonNull(Kind, nameof(Kind));

    public string? SegmentReference { get; } = GovernanceContractValidation.OptionalSafeToken(SegmentReference, nameof(SegmentReference));
}
