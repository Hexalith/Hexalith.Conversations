// <copyright file="AddParticipantBoundary.cs" company="ITANEO">
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
/// Maps public add-participant contracts into the domain aggregate after application-boundary validation.
/// </summary>
public static class AddParticipantBoundary
{
    /// <summary>
    /// Validates command shape before external Party proof is requested.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <returns>A rejection event when command shape is invalid; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateCommandShape(
        AddParticipantCommand? command,
        DateTimeOffset addedAt,
        string eventId)
        => AddParticipantValidation.ValidateShape(command, addedAt, eventId);

    /// <summary>
    /// Validates the schema-shape fields that must be present before the tenant access guard can run.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSchemaShape(AddParticipantCommand? command)
        => AddParticipantValidation.ValidateSchemaShape(command);

    /// <summary>
    /// Validates the semantic-shape fields that must be checked only after the tenant access guard allows the request.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <returns>A rejection event when validation fails; otherwise <see langword="null" />.</returns>
    public static ConversationRejectedDomainEvent? ValidateSemanticShape(
        AddParticipantCommand command,
        DateTimeOffset addedAt,
        string eventId)
        => AddParticipantValidation.ValidateSemanticShape(command, addedAt, eventId);

    /// <summary>
    /// Dispatches a validated public add-participant command through the domain aggregate.
    /// </summary>
    /// <param name="command">The public add-participant command.</param>
    /// <param name="addedAt">The deterministic participant-added timestamp supplied by the boundary.</param>
    /// <param name="eventId">The deterministic event identity supplied by the boundary.</param>
    /// <param name="state">The current conversation state.</param>
    /// <returns>A domain result containing one participant event or one typed rejection.</returns>
    public static DomainResult DispatchValidated(
        AddParticipantCommand command,
        DateTimeOffset addedAt,
        string eventId,
        ConversationState? state)
    {
        AddParticipant domainCommand = new(command, addedAt, eventId);
        return ConversationAggregate.Handle(domainCommand, state);
    }
}
