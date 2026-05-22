// <copyright file="SetConversationRetentionPolicyBoundary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Maps public retention policy commands into the domain aggregate after boundary validation.
/// </summary>
public static class SetConversationRetentionPolicyBoundary
{
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(SetConversationRetentionPolicyCommand? command)
        => SetConversationRetentionPolicyValidation.ValidateSchemaShape(command);

    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        SetConversationRetentionPolicyCommand command,
        string eventId)
        => SetConversationRetentionPolicyValidation.ValidateSemanticShape(command, eventId);

    public static DomainResult DispatchValidated(
        SetConversationRetentionPolicyCommand command,
        GovernanceAuditEvidenceReference auditEvidence,
        string eventId,
        ConversationState? state)
    {
        SetConversationRetentionPolicy domainCommand = new(command, auditEvidence, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
