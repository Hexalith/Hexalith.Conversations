// <copyright file="ConversationRedactionProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Derived redaction read state for authorized conversation details.
/// </summary>
/// <param name="target">The governed target reference.</param>
/// <param name="category">The redaction category.</param>
/// <param name="policyReference">The safe policy reference.</param>
/// <param name="reasonClass">The safe policy reason class.</param>
/// <param name="actorPartyId">The allowed actor attribution.</param>
/// <param name="redactedAt">The redaction timestamp.</param>
/// <param name="auditEvidence">The citeable audit evidence reference.</param>
/// <param name="trustState">The public trust state for this redaction read state.</param>
/// <param name="placeholder">The safe display placeholder.</param>
public sealed record ConversationRedactionProjectionV1(
    GovernanceTarget Target,
    RedactionCategory Category,
    string PolicyReference,
    string ReasonClass,
    PartyId? ActorPartyId,
    DateTimeOffset RedactedAt,
    GovernanceAuditEvidenceReference? AuditEvidence,
    ProjectionTrustState TrustState,
    string Placeholder = "[redacted]")
{
    /// <summary>
    /// Gets the governed target reference.
    /// </summary>
    public GovernanceTarget Target { get; } = Target ?? throw new ArgumentNullException(nameof(Target));

    /// <summary>
    /// Gets the redaction category.
    /// </summary>
    public RedactionCategory Category { get; } = Category ?? throw new ArgumentNullException(nameof(Category));

    /// <summary>
    /// Gets the safe policy reference.
    /// </summary>
    public string PolicyReference { get; } = ValidateRequired(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the safe policy reason class.
    /// </summary>
    public string ReasonClass { get; } = ValidateRequired(ReasonClass, nameof(ReasonClass));

    /// <summary>
    /// Gets the public trust state for this redaction read state.
    /// </summary>
    public ProjectionTrustState TrustState { get; } = TrustState ?? throw new ArgumentNullException(nameof(TrustState));

    /// <summary>
    /// Gets the safe display placeholder.
    /// </summary>
    public string Placeholder { get; } =
        GovernanceContractValidation.RequiredSafeRedactionPlaceholder(Placeholder, nameof(Placeholder));

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
