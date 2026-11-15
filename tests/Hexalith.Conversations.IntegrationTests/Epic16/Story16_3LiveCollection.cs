// <copyright file="Story16_3LiveCollection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>Serializes the five native proof scenarios and owns their isolated infrastructure.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Story16_3LiveCollection : ICollectionFixture<Story16_3LiveFixture>
{
    /// <summary>The native terminal-reconciliation collection.</summary>
    public const string Name = "Story16_3Live";
}
