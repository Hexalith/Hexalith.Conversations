// <copyright file="ReassignConversationProjectBoundary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.Validation;

/// <summary>
/// Maps public project reassignment contracts into the domain aggregate after application-boundary validation.
/// </summary>
public static class ReassignConversationProjectBoundary
{
    /// <summary>
    /// Validates command shape before external boundary checks.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <param name="changedAt">The deterministic project-change timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <returns>A rejection event when command shape is invalid; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateCommandShape(
        ReassignConversationProjectCommand? command,
        DateTimeOffset changedAt,
        string eventId)
        => ReassignConversationProjectValidation.ValidateShape(command, changedAt, eventId);

    /// <summary>
    /// Validates the schema-shape fields that must be present before the tenant access guard can run.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(ReassignConversationProjectCommand? command)
        => ReassignConversationProjectValidation.ValidateSchemaShape(command);

    /// <summary>
    /// Validates the semantic-shape fields that must be checked only after the tenant access guard allows the request.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <param name="changedAt">The deterministic project-change timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        ReassignConversationProjectCommand command,
        DateTimeOffset changedAt,
        string eventId)
        => ReassignConversationProjectValidation.ValidateSemanticShape(command, changedAt, eventId);

    /// <summary>
    /// Dispatches a validated public project reassignment command through the domain aggregate.
    /// </summary>
    /// <param name="command">The public reassignment command.</param>
    /// <param name="changedAt">The deterministic project-change timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one project-changed event, a no-op, or one typed rejection.</returns>
    public static DomainResult DispatchValidated(
        ReassignConversationProjectCommand command,
        DateTimeOffset changedAt,
        string eventId,
        ConversationState? state)
    {
        ReassignConversationProject domainCommand = new(command, changedAt, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
