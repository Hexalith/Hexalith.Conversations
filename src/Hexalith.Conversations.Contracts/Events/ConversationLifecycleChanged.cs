// <copyright file="ConversationLifecycleChanged.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Records a bounded lifecycle transition for a conversation.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="previousState">The previous lifecycle state.</param>
/// <param name="currentState">The current lifecycle state.</param>
/// <param name="reasonCode">An optional safe reason code.</param>
public sealed record ConversationLifecycleChanged(
    ConversationEventMetadata Metadata,
    ConversationLifecycleStatus PreviousState,
    ConversationLifecycleStatus CurrentState,
    string? ReasonCode = null)
{
    /// <summary>
    /// Gets the public event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the previous lifecycle state.
    /// </summary>
    public ConversationLifecycleStatus PreviousState { get; } = PreviousState ?? throw new ArgumentNullException(nameof(PreviousState));

    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public ConversationLifecycleStatus CurrentState { get; } = CurrentState ?? throw new ArgumentNullException(nameof(CurrentState));
}
