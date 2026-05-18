// <copyright file="IdentifierValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

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
        foreach (string? value in new[] { null, string.Empty, " ", "\t", "\n" })
        {
            Should.Throw<ArgumentException>(() => new ConversationId(value!));
            Should.Throw<ArgumentException>(() => new TenantId(value!));
            Should.Throw<ArgumentException>(() => new PartyId(value!));
            Should.Throw<ArgumentException>(() => new ProjectId(value!));
            Should.Throw<ArgumentException>(() => new FolderId(value!));
            Should.Throw<ArgumentException>(() => new FileId(value!));
            Should.Throw<ArgumentException>(() => new MessageId(value!));
            Should.Throw<ArgumentException>(() => new BusinessReference("crm", value!));
            Should.Throw<ArgumentException>(() => ProjectionTrustState.Parse(value!));
        }

        Should.Throw<ArgumentOutOfRangeException>(() => new SchemaVersion(0));
        Should.Throw<ArgumentOutOfRangeException>(() => new SchemaVersion(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => new SchemaVersion(int.MinValue));
        new SchemaVersion(int.MaxValue).Value.ShouldBe(int.MaxValue);
    }

    /// <summary>
    /// Ensures provider correlation metadata remains separate from conversation identity.
    /// </summary>
    [Fact]
    public void ProviderCorrelationShouldNotReplaceConversationIdentity()
    {
        typeof(ProviderCorrelationMetadata)
            .GetProperty(nameof(ProviderCorrelationMetadata.ProviderSessionReference))!
            .PropertyType
            .ShouldNotBe(typeof(ConversationId));

        typeof(ProviderCorrelationMetadata)
            .GetProperty(nameof(ProviderCorrelationMetadata.ProviderResponseReference))!
            .PropertyType
            .ShouldNotBe(typeof(ConversationId));
    }

    /// <summary>
    /// Ensures malformed JSON cannot bypass identifier validation.
    /// </summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("\"  \"")]
    public void IdentifierJsonShouldRejectMalformedValues(string json)
    {
        if (json == "null")
        {
            JsonSerializer.Deserialize<ConversationId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<TenantId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<PartyId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<ProjectId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<FolderId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<FileId>(json).ShouldBeNull();
            JsonSerializer.Deserialize<MessageId>(json).ShouldBeNull();
            return;
        }

        Should.Throw<Exception>(() => JsonSerializer.Deserialize<ConversationId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<TenantId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<PartyId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<ProjectId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<FolderId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<FileId>(json));
        Should.Throw<Exception>(() => JsonSerializer.Deserialize<MessageId>(json));
    }

    /// <summary>
    /// Documents that flat primitive JSON uses the destination contract type as the type boundary.
    /// </summary>
    [Fact]
    public void FlatIdentifierJsonShouldDependOnDestinationContractType()
    {
        string json = JsonSerializer.Serialize(new TenantId("tenant-001"));

        ConversationId conversationId = JsonSerializer.Deserialize<ConversationId>(json)!;

        conversationId.Value.ShouldBe("tenant-001");
        conversationId.ShouldBeOfType<ConversationId>();
    }
}
