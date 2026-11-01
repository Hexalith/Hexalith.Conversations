// <copyright file="Story16_1LiveCollection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>Serializes the Story 16.1 live lane around its two shared sidecars.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Story16_1LiveCollection : ICollectionFixture<Story16_1LiveFixture>
{
    /// <summary>The collection name.</summary>
    public const string Name = "Story 16.1 live durable tenant projection";
}
