// <copyright file="IdentifierValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies required public value contracts reject ambiguous empty values.
/// </summary>
public sealed class IdentifierValidationTest
{
    /// <summary>
    /// Ensures identifier constructors reject empty values.
    /// </summary>
    [Fact]
    public void StableIdentityContractsShouldRejectEmptyValues()
    {
        Should.Throw<ArgumentException>(() => new ConversationId(string.Empty));
        Should.Throw<ArgumentException>(() => new TenantId(" "));
        Should.Throw<ArgumentException>(() => new PartyId(string.Empty));
        Should.Throw<ArgumentException>(() => new ProjectId(string.Empty));
        Should.Throw<ArgumentException>(() => new FolderId(string.Empty));
        Should.Throw<ArgumentException>(() => new FileId(string.Empty));
        Should.Throw<ArgumentException>(() => new MessageId(string.Empty));
        Should.Throw<ArgumentException>(() => new BusinessReference("crm", string.Empty));
        Should.Throw<ArgumentException>(() => new ProjectionTrustState(string.Empty));
        Should.Throw<ArgumentOutOfRangeException>(() => new SchemaVersion(0));
    }

    /// <summary>
    /// Ensures provider correlation metadata remains separate from conversation identity.
    /// </summary>
    [Fact]
    public void ProviderCorrelationShouldNotReplaceConversationIdentity()
    {
        ContractSamples.ProviderCorrelation.ProviderSessionReference.ShouldNotBe(ContractSamples.Conversation.Value);
        ContractSamples.ProviderCorrelation.ProviderResponseReference.ShouldNotBe(ContractSamples.Conversation.Value);
    }
}
