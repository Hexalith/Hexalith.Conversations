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
/// Verifies required public value contracts reject ambiguous empty values and cross-type substitution.
/// </summary>
public sealed class IdentifierValidationTest
{
    /// <summary>
    /// Ensures identifier constructors reject null and whitespace values across every whitespace variant.
    /// </summary>
    [Fact]
    public void StableIdentityContractsShouldRejectEmptyValues()
    {
        foreach (string? value in new[] { null, string.Empty, " ", "\t", "\n", "\r\n" })
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
    /// Asserts that <see cref="ProviderCorrelationMetadata"/> cannot accidentally satisfy
    /// <see cref="ConversationId"/>'s contract by sharing a wire-shape or property layout.
    /// </summary>
    [Fact]
    public void ProviderCorrelationShouldNotReplaceConversationIdentity()
    {
        string providerJson = JsonSerializer.Serialize(
            new ProviderCorrelationMetadata("provider-a", "assistant", new SchemaVersion(1)));

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationId>(providerJson));

        string conversationJson = JsonSerializer.Serialize(new ConversationId("conversation-001"));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProviderCorrelationMetadata>(conversationJson));
    }

    /// <summary>
    /// Pins the wire shape: identifier JSON is a prefixed URN-style string. Top-level null is short-circuited
    /// to a C# null by System.Text.Json; structural malformed inputs throw <see cref="JsonException"/>.
    /// </summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("\"  \"")]
    [InlineData("\"tenant-001\"")] // missing prefix
    [InlineData("\"conv:\"")] // empty value after prefix
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

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TenantId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PartyId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<FolderId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<FileId>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<MessageId>(json));
    }

    /// <summary>
    /// Ensures that the JSON of one identifier family cannot be silently rehydrated as another.
    /// The per-type URN prefix prevents silent cross-type substitution on the wire.
    /// </summary>
    [Fact]
    public void IdentifierJsonShouldRejectCrossTypeRehydration()
    {
        string tenantJson = JsonSerializer.Serialize(new TenantId("tenant-001"));
        tenantJson.ShouldBe("\"tenant:tenant-001\"");

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationId>(tenantJson));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PartyId>(tenantJson));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectId>(tenantJson));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<FolderId>(tenantJson));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<FileId>(tenantJson));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<MessageId>(tenantJson));

        string conversationJson = JsonSerializer.Serialize(new ConversationId("conv-001"));
        conversationJson.ShouldBe("\"conv:conv-001\"");
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TenantId>(conversationJson));
    }

    /// <summary>
    /// Pins the wire prefix per identifier family.
    /// </summary>
    [Fact]
    public void IdentifierJsonShouldUseCanonicalPerTypePrefix()
    {
        JsonSerializer.Serialize(new TenantId("x")).ShouldBe("\"tenant:x\"");
        JsonSerializer.Serialize(new ConversationId("x")).ShouldBe("\"conv:x\"");
        JsonSerializer.Serialize(new PartyId("x")).ShouldBe("\"party:x\"");
        JsonSerializer.Serialize(new ProjectId("x")).ShouldBe("\"project:x\"");
        JsonSerializer.Serialize(new FolderId("x")).ShouldBe("\"folder:x\"");
        JsonSerializer.Serialize(new FileId("x")).ShouldBe("\"file:x\"");
        JsonSerializer.Serialize(new MessageId("x")).ShouldBe("\"message:x\"");
    }

    /// <summary>
    /// Pins the canonical wire shape for <see cref="SchemaVersion"/> as a strict JSON integer.
    /// Fractional numbers, exponent notation, and string-wrapped integers are rejected.
    /// </summary>
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.5")]
    [InlineData("\"1\"")]
    public void SchemaVersionShouldRejectNonIntegerJson(string json)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>(json));

    /// <summary>
    /// Schema version JSON converter must surface invalid values as <see cref="JsonException"/>, not <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void SchemaVersionShouldRejectInvalidIntegersAsJsonException(string json)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>(json));

    /// <summary>
    /// Closed-vocabulary <see cref="Parse"/> is case-sensitive on canonical PascalCase / snake_case values.
    /// </summary>
    [Fact]
    public void ClosedVocabularyShouldRejectCaseVariants()
    {
        Should.Throw<ArgumentException>(() => ProjectionTrustState.Parse("current"));
        Should.Throw<ArgumentException>(() => ProjectionTrustState.Parse("CURRENT"));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectionTrustState>("\"current\""));
    }

    /// <summary>
    /// <see cref="PartyId"/> normalizes whitespace and casing at construction so substitution guards
    /// cannot be bypassed by typographic variants of the same identity. The wire prefix is unaffected.
    /// </summary>
    [Theory]
    [InlineData("Party-One", "party-one")]
    [InlineData(" Party-One ", "party-one")]
    [InlineData("PARTY-ONE", "party-one")]
    [InlineData("\tparty-one\n", "party-one")]
    public void PartyIdShouldNormalizeWhitespaceAndCasing(string input, string expectedNormalized)
    {
        new PartyId(input).Value.ShouldBe(expectedNormalized);
        new PartyId(input).ShouldBe(new PartyId(expectedNormalized));
        JsonSerializer.Serialize(new PartyId(input)).ShouldBe($"\"party:{expectedNormalized}\"");
    }
}
