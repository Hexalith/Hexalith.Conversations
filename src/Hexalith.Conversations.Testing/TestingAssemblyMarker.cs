// <copyright file="TestingAssemblyMarker.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts;

namespace Hexalith.Conversations.Testing;

/// <summary>
/// Marks the testing helper assembly for future synthetic fixtures.
/// </summary>
public static class TestingAssemblyMarker
{
    /// <summary>
    /// Gets the contracts marker type to keep helper dependencies explicit.
    /// </summary>
    public static Type ContractsMarkerType => typeof(ContractsAssemblyMarker);
}
