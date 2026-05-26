// <copyright file="ConversationProjectAssignment.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Commands;

/// <summary>
/// Carries an explicit project assignment or clear operation for a conversation.
/// </summary>
/// <param name="operation">The explicit operation.</param>
/// <param name="projectId">The target project for assign operations; omitted for clear operations.</param>
public sealed record ConversationProjectAssignment(
    ConversationProjectAssignmentOperation Operation,
    ProjectId? ProjectId = null)
{
    /// <summary>
    /// Gets the explicit operation.
    /// </summary>
    public ConversationProjectAssignmentOperation Operation { get; } =
        Operation ?? throw new ArgumentNullException(nameof(Operation));
}
