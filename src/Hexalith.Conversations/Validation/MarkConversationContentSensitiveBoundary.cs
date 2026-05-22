// <copyright file="MarkConversationContentSensitiveBoundary.cs" company="ITANEO">
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
/// Maps public sensitivity commands into the domain aggregate after boundary validation.
/// </summary>
public static class MarkConversationContentSensitiveBoundary
{
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(MarkConversationContentSensitiveCommand? command)
        => MarkConversationContentSensitiveValidation.ValidateSchemaShape(command);

    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        MarkConversationContentSensitiveCommand command,
        string eventId)
        => MarkConversationContentSensitiveValidation.ValidateSemanticShape(command, eventId);

    public static DomainResult DispatchValidated(
        MarkConversationContentSensitiveCommand command,
        GovernanceAuditEvidenceReference auditEvidence,
        string eventId,
        ConversationState? state)
    {
        MarkConversationContentSensitive domainCommand = new(command, auditEvidence, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
