// <copyright file="PortableCompileSurfaceValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Conformance.Tests.Story92;

namespace Hexalith.Conversations.Conformance.Portable.Tests;

/// <summary>Proves portability from evaluated packability, transitive assets, and ReferencePath.</summary>
public sealed class PortableCompileSurfaceValidationTest
{
    /// <summary>AC-9.2-02: resolved compile dependencies contain no non-packable module reference.</summary>
    [Fact]
    public void ResolvedSurfaceShouldContainNoNonPackableModuleReference()
        => Story92TieringVerification.Verify("--surface-only");
}
