// <copyright file="ConformanceOracleTieringValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Conformance.Tests.Story92;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Live Story 9.2 controls verify both evaluated compilations and their exact declarations.</summary>
public sealed class ConformanceOracleTieringValidationTest
{
    /// <summary>AC-9.2-04: preserves all frozen identities and approved before/successor strengths.</summary>
    [Fact]
    public void PostSplitAssertionInventoryShouldEqualApprovedDisposition()
        => Story92TieringVerification.Verify("--structure-only");

    /// <summary>AC-9.2-05: both tiers occur once in the solution and completion/CI inventories.</summary>
    [Fact]
    public void BothTiersShouldBeDeclaredEverywhere()
        => Story92TieringVerification.Verify("--declarations-only");
}
