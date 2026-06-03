// <copyright file="ReleaseEvidenceArtifactCollection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Serializes every test that reads or writes a committed <c>docs/release-evidence/*</c> artifact so a
/// generation test (writer) never runs concurrently with a validation test (reader) of the same file.
/// </summary>
/// <remarks>
/// Story 2.1 closes Epic 1 retro carry-forward T1 (HIGH): under xUnit parallelism the public-contract-shape
/// snapshot generator (writer) and the release-baseline validator (reader) could interleave on the same file,
/// throwing a transient <c>JsonReaderException</c>. Epic 2 raises gate-run frequency, so this is the owning
/// moment to fix it test-only. <see cref="CollectionDefinitionAttribute"/> with parallelization disabled puts
/// all readers and writers of these committed evidence files into a single sequential collection; the rest of
/// the conformance suite keeps running in parallel. This is a test-execution-ordering fix only — no assertion
/// strength is changed and no committed artifact is altered.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ReleaseEvidenceArtifactCollection
{
    /// <summary>The shared collection name for release-evidence file readers and writers.</summary>
    public const string Name = "ReleaseEvidenceArtifacts";
}
