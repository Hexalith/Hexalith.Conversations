// <copyright file="GovernanceTarget.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

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
/// <param name="auditEvidenceHandle">An optional opaque audit evidence reference for governed audit records.</param>
public sealed record GovernanceTarget(
    GovernedTargetKind Kind,
    MessageId? MessageId = null,
    FileId? FileId = null,
    PartyId? PartyId = null,
    string? SegmentReference = null,
    AuditEvidenceHandle? AuditEvidenceHandle = null)
{
    public GovernedTargetKind Kind { get; } = GovernanceContractValidation.RequireNonNull(Kind, nameof(Kind));

    public string? SegmentReference { get; } = GovernanceContractValidation.OptionalSafeToken(SegmentReference, nameof(SegmentReference));

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuditEvidenceHandle? AuditEvidenceHandle { get; } = AuditEvidenceHandle;

    /// <summary>
    /// Builds the deterministic safe key used to identify this target across aggregate replay and
    /// projection materialization. Centralizing here prevents key drift between writers and readers.
    /// </summary>
    /// <returns>The deterministic safe target key.</returns>
    public string ToTargetKey()
    {
        if (Kind == GovernedTargetKind.Conversation)
        {
            return "conversation";
        }

        if (Kind == GovernedTargetKind.Message)
        {
            return $"message:{MessageId?.Value}";
        }

        if (Kind == GovernedTargetKind.File)
        {
            return $"file:{FileId?.Value}";
        }

        if (Kind == GovernedTargetKind.Participant)
        {
            return $"participant:{PartyId?.Value}";
        }

        if (Kind == GovernedTargetKind.ContentSegment)
        {
            return $"segment:{SegmentReference}";
        }

        if (Kind == GovernedTargetKind.AuditRecord)
        {
            if (AuditEvidenceHandle is null)
            {
                throw new InvalidOperationException("Audit-record targets require a safe audit evidence handle.");
            }

            return $"audit:{AuditEvidenceHandle.Value}";
        }

        return $"unsupported:{Kind.Value}";
    }
}
