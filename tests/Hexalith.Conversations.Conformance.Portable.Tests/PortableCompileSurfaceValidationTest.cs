// <copyright file="PortableCompileSurfaceValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Conformance.Portable.Tests;

/// <summary>Proves portability from evaluated packability, transitive assets, and ReferencePath.</summary>
public sealed class PortableCompileSurfaceValidationTest
{
    /// <summary>Resolved compile dependencies contain no non-packable module reference.</summary>
    [Fact]
    public void ResolvedSurfaceShouldContainNoNonPackableModuleReference()
        => PortableSurfaceVerification.Verify();

    /// <summary>Dependency drift remains rejected after reference reconciliation.</summary>
    [Fact]
    public void DependencyBoundaryControlsShouldRejectDrift()
        => PortableSurfaceVerification.Verify("tests/tooling/test_portable_surface.py", "PASS: portable dependency refusal controls");
}
