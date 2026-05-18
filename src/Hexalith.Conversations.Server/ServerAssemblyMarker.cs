// <copyright file="ServerAssemblyMarker.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

namespace Hexalith.Conversations.Server;

/// <summary>
/// Marks the server boundary assembly without registering runtime behavior.
/// </summary>
public static class ServerAssemblyMarker
{
    /// <summary>
    /// Gets the contracts marker type for boundary smoke tests.
    /// </summary>
    public static Type ContractsMarkerType => typeof(ContractsAssemblyMarker);

    /// <summary>
    /// Gets the domain marker type for boundary smoke tests.
    /// </summary>
    public static Type DomainMarkerType => typeof(ConversationsAssemblyMarker);
}
