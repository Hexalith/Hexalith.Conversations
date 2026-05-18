// <copyright file="ConversationTestIdsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Testing.Factories;

namespace Hexalith.Conversations.Tests.Testing;

/// <summary>
/// Verifies deterministic test identifier factories.
/// </summary>
public sealed class ConversationTestIdsTest
{
    /// <summary>
    /// Ensures generated identifiers are stable and readable.
    /// </summary>
    [Theory]
    [InlineData("Tenant Isolation Revoked", "tenant-isolation-revoked")]
    [InlineData("Audit: Redaction Replay", "audit-redaction-replay")]
    public void TenantShouldCreateStableReadableIdentifier(string scenario, string expectedSuffix)
    {
        string id = ConversationTestIds.Tenant(scenario);

        id.ShouldBe($"tenant-{expectedSuffix}");
    }

    /// <summary>
    /// Ensures invalid scenarios fail at the test boundary.
    /// </summary>
    [Fact]
    public void ConversationShouldRejectBlankScenario()
    {
        Should.Throw<ArgumentException>(() => ConversationTestIds.Conversation(" "));
    }
}

