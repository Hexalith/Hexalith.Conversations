// <copyright file="PersistedConversationEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Commons.Publication;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Carries the result of the persistence boundary into the Conversations-safe publication mapper.
/// </summary>
/// <param name="Outcome">The persistence outcome.</param>
/// <param name="TenantId">The tenant scope validated by the command/persistence path.</param>
/// <param name="Payload">The durable event payload or rejected outcome payload.</param>
public sealed record PersistedConversationEvent(
    ConversationPersistenceOutcome Outcome,
    TenantId TenantId,
    object Payload)
{
    /// <summary>
    /// Creates a successful persisted event candidate.
    /// </summary>
    /// <param name="tenantId">The validated tenant scope.</param>
    /// <param name="payload">The durable event payload.</param>
    /// <returns>The persisted event candidate.</returns>
    public static PersistedConversationEvent Success(TenantId tenantId, object payload)
        => new(ConversationPersistenceOutcome.Succeeded, tenantId, payload);

    /// <summary>
    /// Converts the Conversations envelope to the shared publication candidate shape.
    /// </summary>
    /// <returns>The shared publication candidate.</returns>
    public PersistedPublicationCandidate<ConversationPersistenceOutcome, TenantId> ToPublicationCandidate()
        => new(Outcome, TenantId, Payload);
}
