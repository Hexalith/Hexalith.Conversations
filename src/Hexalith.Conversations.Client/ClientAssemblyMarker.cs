// <copyright file="ClientAssemblyMarker.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Marks the adopter client assembly without exposing implementation or infrastructure contracts.
/// </summary>
public static class ClientAssemblyMarker
{
    /// <summary>
    /// Gets the contracts marker type to keep the public contract dependency explicit.
    /// </summary>
    public static Type ContractsMarkerType => typeof(ContractsAssemblyMarker);
}
