// <copyright file="ConversationProjectionConsistencyException.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Signals that the detail and tenant-index keys do not prove the same completed generation.
/// </summary>
internal sealed class ConversationProjectionConsistencyException : Exception
{
    internal ConversationProjectionConsistencyException()
        : base("The conversation read-model generation is not complete.")
    {
    }
}
