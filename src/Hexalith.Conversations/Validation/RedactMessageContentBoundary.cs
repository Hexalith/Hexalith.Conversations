// <copyright file="RedactMessageContentBoundary.cs" company="ITANEO">
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
/// Maps public redaction commands into the domain aggregate after boundary validation.
/// </summary>
public static class RedactMessageContentBoundary
{
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(RedactMessageContentCommand? command)
        => RedactMessageContentValidation.ValidateSchemaShape(command);

    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        RedactMessageContentCommand command,
        string eventId)
        => RedactMessageContentValidation.ValidateSemanticShape(command, eventId);

    public static ConversationRejectedDomainEvent? ValidateStateBeforeAudit(
        RedactMessageContentCommand command,
        string eventId,
        ConversationState? state)
        => RedactMessageContentValidation.ValidateStateBeforeAudit(command, eventId, state);

    public static bool IsCompatibleExistingRedaction(RedactMessageContentCommand command, ConversationState? state)
        => RedactMessageContentValidation.IsCompatibleExistingRedaction(command, state);

    public static ConversationRejectedDomainEvent? ValidateAuditEvidenceProvided(
        RedactMessageContentCommand command,
        GovernanceAuditEvidenceReference? auditEvidence)
        => RedactMessageContentValidation.ValidateAuditEvidenceProvided(command, auditEvidence);

    public static DomainResult DispatchValidated(
        RedactMessageContentCommand command,
        GovernanceAuditEvidenceReference auditEvidence,
        string eventId,
        ConversationState? state)
    {
        RedactMessageContent domainCommand = new(command, auditEvidence, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
