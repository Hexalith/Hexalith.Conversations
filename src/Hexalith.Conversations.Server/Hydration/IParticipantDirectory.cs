// <copyright file="IParticipantDirectory.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Validates command-time participant Party references at the application boundary.
/// </summary>
public interface IParticipantDirectory
{
    /// <summary>
    /// Validates that a participant Party reference is usable for the command tenant.
    /// </summary>
    /// <param name="tenantId">The command tenant binding.</param>
    /// <param name="participantPartyId">The target participant Party reference.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A content-safe validation result.</returns>
    ValueTask<ParticipantDirectoryValidation> ValidateParticipantAsync(
        TenantId tenantId,
        PartyId participantPartyId,
        CancellationToken cancellationToken = default);
}
