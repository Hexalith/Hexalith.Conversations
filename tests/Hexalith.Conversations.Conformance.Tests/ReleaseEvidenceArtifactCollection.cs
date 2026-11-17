// <copyright file="ReleaseEvidenceArtifactCollection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Serializes product API fixture readers and round-trip checks for deterministic execution.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ReleaseEvidenceArtifactCollection
{
    /// <summary>The shared collection name for product fixture readers and round-trip checks.</summary>
    public const string Name = "ReleaseEvidenceArtifacts";
}
