// <copyright file="AddAgentParticipant.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Commands;

/// <summary>Pure restricted AddAgentParticipant intent; admission supplies current authority.</summary>
/// <param name="PublicCommand">The portable owner contract.</param>
/// <param name="AddedAt">Deterministic occurrence time.</param>
/// <param name="EventId">Deterministic source event identity.</param>
public sealed record AddAgentParticipant(AddParticipantCommand PublicCommand, DateTimeOffset AddedAt, string EventId);
