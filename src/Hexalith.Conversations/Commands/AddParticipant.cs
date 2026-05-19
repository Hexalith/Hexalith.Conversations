// <copyright file="AddParticipant.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for adding a validated participant to a tenant-scoped conversation.
/// </summary>
/// <param name="PublicCommand">The public add-participant contract supplied by an adopter boundary.</param>
/// <param name="AddedAt">The deterministic participant-added timestamp supplied by the boundary.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record AddParticipant(
    AddParticipantCommand PublicCommand,
    DateTimeOffset AddedAt,
    string EventId);
