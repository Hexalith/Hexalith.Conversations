// <copyright file="SecondAdopterVocabularyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the <see cref="SecondAdopterStatus"/> closed vocabulary for completeness,
/// parse correctness, unknown-value rejection, and JSON round-trip.
/// </summary>
public sealed class SecondAdopterVocabularyTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void SecondAdopterStatus_AllContains4Values()
    {
        SecondAdopterStatus.All.Count.ShouldBe(4);
        string[] expected = ["identified", "qualified", "deferred", "disqualified"];
        SecondAdopterStatus.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void SecondAdopterStatus_Parse_Identified_ReturnsIdentified()
    {
        SecondAdopterStatus result = SecondAdopterStatus.Parse("identified");
        result.ShouldBe(SecondAdopterStatus.Identified);
    }

    [Fact]
    public void SecondAdopterStatus_Parse_Qualified_ReturnsQualified()
    {
        SecondAdopterStatus result = SecondAdopterStatus.Parse("qualified");
        result.ShouldBe(SecondAdopterStatus.Qualified);
    }

    [Fact]
    public void SecondAdopterStatus_Parse_Deferred_ReturnsDeferred()
    {
        SecondAdopterStatus result = SecondAdopterStatus.Parse("deferred");
        result.ShouldBe(SecondAdopterStatus.Deferred);
    }

    [Fact]
    public void SecondAdopterStatus_Parse_Disqualified_ReturnsDisqualified()
    {
        SecondAdopterStatus result = SecondAdopterStatus.Parse("disqualified");
        result.ShouldBe(SecondAdopterStatus.Disqualified);
    }

    [Fact]
    public void SecondAdopterStatus_Parse_UnknownValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => SecondAdopterStatus.Parse("pending"));
    }

    [Fact]
    public void SecondAdopterStatus_SerializesAndDeserializesToCorrectValue()
    {
        foreach (SecondAdopterStatus status in SecondAdopterStatus.All)
        {
            string json = JsonSerializer.Serialize(status, WebOptions);
            json.ShouldBe($"\"{status.Value}\"");

            SecondAdopterStatus? parsed = JsonSerializer.Deserialize<SecondAdopterStatus>(json, WebOptions);
            parsed.ShouldNotBeNull();
            parsed.ShouldBe(status);
        }
    }

    [Fact]
    public void SecondAdopterStatus_Disqualified_WireValueIsDisqualified()
    {
        string json = JsonSerializer.Serialize(SecondAdopterStatus.Disqualified, WebOptions);
        json.ShouldBe("\"disqualified\"");
    }
}
