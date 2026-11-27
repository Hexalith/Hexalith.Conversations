// <copyright file="DerivedKeyPriorBinaryLoadContext.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Runtime.Loader;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Isolated <see cref="AssemblyLoadContext"/> that loads the compiled pre-Story-12.3
/// <c>Hexalith.Conversations.Server</c> binary while sharing Contracts and EventStore SDK assemblies from
/// <see cref="AssemblyLoadContext.Default"/> so both binaries can operate against the same store and contracts.
/// </summary>
internal sealed class DerivedKeyPriorBinaryLoadContext()
    : AssemblyLoadContext("Story12_3PriorServerRollback", isCollectible: false)
{
    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
