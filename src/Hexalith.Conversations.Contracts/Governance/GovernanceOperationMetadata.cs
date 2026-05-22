// <copyright file="GovernanceOperationMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Carries public governance operation authority, policy, timestamp, and correlation evidence.
/// </summary>
/// <remarks>
/// This metadata complements <see cref="ConversationCommandMetadata"/>. It keeps command idempotency
/// separate from governance rationale and audit evidence identity.
/// </remarks>
/// <param name="schemaVersion">The governance contract schema version.</param>
/// <param name="tenantId">The tenant scope for the governed operation.</param>
/// <param name="conversationId">The governed conversation identity.</param>
/// <param name="actorPartyId">The stable Party actor attribution.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="policyReference">The required content-safe policy reference.</param>
/// <param name="operationTimestamp">The UTC operation timestamp supplied as contract evidence.</param>
/// <param name="correlationId">The required caller correlation identifier.</param>
/// <param name="causationId">The optional causation identifier.</param>
public sealed record GovernanceOperationMetadata(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    PartyId ActorPartyId,
    string Rationale,
    string PolicyReference,
    DateTimeOffset OperationTimestamp,
    string CorrelationId,
    string? CausationId = null)
{
    public SchemaVersion SchemaVersion { get; } = GovernanceContractValidation.RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    public TenantId TenantId { get; } = GovernanceContractValidation.RequireNonNull(TenantId, nameof(TenantId));

    public ConversationId ConversationId { get; } = GovernanceContractValidation.RequireNonNull(ConversationId, nameof(ConversationId));

    public PartyId ActorPartyId { get; } = GovernanceContractValidation.RequireNonNull(ActorPartyId, nameof(ActorPartyId));

    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public DateTimeOffset OperationTimestamp { get; } = GovernanceContractValidation.RequiredUtcTimestamp(OperationTimestamp, nameof(OperationTimestamp));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public string? CausationId { get; } = GovernanceContractValidation.OptionalSafeToken(CausationId, nameof(CausationId));

    /// <summary>
    /// Creates governance metadata from command metadata while requiring the governed conversation and policy rationale.
    /// </summary>
    /// <param name="metadata">The command metadata to map from.</param>
    /// <param name="conversationId">The governed conversation identity.</param>
    /// <param name="rationale">The content-safe governance rationale.</param>
    /// <param name="policyReference">The content-safe policy reference.</param>
    /// <param name="operationTimestamp">The UTC operation timestamp.</param>
    /// <returns>The governance operation metadata.</returns>
    public static GovernanceOperationMetadata FromCommandMetadata(
        ConversationCommandMetadata metadata,
        ConversationId conversationId,
        string rationale,
        string policyReference,
        DateTimeOffset operationTimestamp)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return new(
            metadata.SchemaVersion,
            metadata.TenantId,
            conversationId,
            metadata.ActorPartyId,
            rationale,
            policyReference,
            operationTimestamp,
            metadata.CorrelationId,
            metadata.CausationId);
    }

    // Rationale and PolicyReference are deliberately omitted to keep ToString content-safe.
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(GovernanceOperationMetadata))
            .Append(" { SchemaVersion = ").Append(SchemaVersion)
            .Append(", TenantId = ").Append(TenantId)
            .Append(", ConversationId = ").Append(ConversationId)
            .Append(", ActorPartyId = ").Append(ActorPartyId)
            .Append(", OperationTimestamp = ").Append(OperationTimestamp.ToString("O"))
            .Append(", CorrelationId = ").Append(CorrelationId)
            .Append(", CausationId = ").Append(CausationId ?? "<none>")
            .Append(" }");
        return builder.ToString();
    }
}
