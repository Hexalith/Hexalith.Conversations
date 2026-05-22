// <copyright file="GovernanceAuditEvidenceReference.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Cites audit evidence without exposing audit persistence locations, diagnostics, or implementation names.
/// </summary>
/// <param name="handle">The opaque audit evidence handle.</param>
/// <param name="policyReference">The policy reference associated with the evidence.</param>
/// <param name="capturedAt">The UTC timestamp when the evidence was captured.</param>
public sealed record GovernanceAuditEvidenceReference(
    AuditEvidenceHandle Handle,
    string PolicyReference,
    DateTimeOffset CapturedAt)
{
    public AuditEvidenceHandle Handle { get; } = GovernanceContractValidation.RequireNonNull(Handle, nameof(Handle));

    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public DateTimeOffset CapturedAt { get; } = GovernanceContractValidation.RequiredUtcTimestamp(CapturedAt, nameof(CapturedAt));

    // PolicyReference is deliberately omitted to keep ToString content-safe.
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(GovernanceAuditEvidenceReference))
            .Append(" { Handle = ").Append(Handle)
            .Append(", CapturedAt = ").Append(CapturedAt.ToString("O"))
            .Append(" }");
        return builder.ToString();
    }
}
