// <copyright file="ReassignConversationProject.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Commands;

/// <summary>
/// Domain command for changing a conversation's stable project reference.
/// </summary>
/// <param name="PublicCommand">The public reassignment contract supplied by an adopter boundary.</param>
/// <param name="ChangedAt">The deterministic project-change timestamp supplied by the boundary.</param>
/// <param name="EventId">The deterministic public event identity supplied by the boundary.</param>
public sealed record ReassignConversationProject(
    ReassignConversationProjectCommand PublicCommand,
    DateTimeOffset ChangedAt,
    string EventId);
