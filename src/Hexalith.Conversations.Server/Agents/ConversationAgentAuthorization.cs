// <copyright file="ConversationAgentAuthorization.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Current exact-operation authority and independently verified immutable Party.</summary>
/// <param name="Outcome">Available only when current authority was established.</param>
/// <param name="TenantId">Authenticated tenant.</param>
/// <param name="PrincipalId">Authenticated principal.</param>
/// <param name="PartyId">Exact immutable provisioned Organization Party for service operations.</param>
/// <param name="AuthorityRevision">Current opaque authority revision.</param>
/// <param name="OrganizationIdentityConfirmed">True only for independently resolved exact active Organization.</param>
public sealed record ConversationAgentAuthorization(ConversationAgentsOutcome Outcome, TenantId? TenantId = null,
    string? PrincipalId = null, PartyId? PartyId = null, string? AuthorityRevision = null,
    bool OrganizationIdentityConfirmed = false);
