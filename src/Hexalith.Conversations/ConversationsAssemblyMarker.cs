// <copyright file="ConversationsAssemblyMarker.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

namespace Hexalith.Conversations;

/// <summary>
/// Marks the deterministic domain assembly for future conversation behavior.
/// </summary>
public static class ConversationsAssemblyMarker
{
    /// <summary>
    /// Gets the contracts marker type to keep the domain-to-contracts dependency explicit.
    /// </summary>
    public static Type ContractsMarkerType => typeof(ContractsAssemblyMarker);
}
