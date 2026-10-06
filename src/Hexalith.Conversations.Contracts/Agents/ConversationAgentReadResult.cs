// <copyright file="ConversationAgentReadResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>A complete current source result; denied/unavailable results contain no source content.</summary>
/// <param name="Outcome">The classified result.</param>
/// <param name="TenantId">The authenticated tenant.</param>
/// <param name="ConversationId">The authenticated source identity.</param>
/// <param name="SourceRevision">Inclusive complete source high-water.</param>
/// <param name="ObservedAt">Authoritative source observation time.</param>
/// <param name="ObservationId">Opaque source checkpoint evidence.</param>
/// <param name="Participants">Complete current roster, including Facilitator.</param>
/// <param name="Messages">Complete current visible messages or the exact lookup message.</param>
/// <param name="AgentParticipantPresent">Whether the immutable AI participant is currently present.</param>
/// <param name="SourceContractVersion">Closed V1 source contract.</param>
public sealed record ConversationAgentReadResult(ConversationAgentsOutcome Outcome, TenantId TenantId,
    ConversationId ConversationId, long? SourceRevision = null, DateTimeOffset? ObservedAt = null,
    string? ObservationId = null, IReadOnlyList<ConversationParticipantProjectionV1>? Participants = null,
    IReadOnlyList<ConversationAgentMessage>? Messages = null, bool AgentParticipantPresent = false, int SourceContractVersion = 1);
