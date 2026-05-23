// <copyright file="BuyerAcceptanceVocabularyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the <see cref="BuyerAcceptanceItemStatus"/> closed vocabulary for completeness,
/// parse correctness, unknown-value rejection, and JSON round-trip.
/// </summary>
public sealed class BuyerAcceptanceVocabularyTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void BuyerAcceptanceItemStatus_AllContains4Values()
    {
        BuyerAcceptanceItemStatus.All.Count.ShouldBe(4);
        string[] expected = ["accepted", "excluded", "unknown-accepted", "waived"];
        BuyerAcceptanceItemStatus.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_Parse_Accepted_ReturnsAccepted()
    {
        BuyerAcceptanceItemStatus result = BuyerAcceptanceItemStatus.Parse("accepted");
        result.ShouldBe(BuyerAcceptanceItemStatus.Accepted);
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_Parse_Excluded_ReturnsExcluded()
    {
        BuyerAcceptanceItemStatus result = BuyerAcceptanceItemStatus.Parse("excluded");
        result.ShouldBe(BuyerAcceptanceItemStatus.Excluded);
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_Parse_UnknownAccepted_ReturnsUnknownAccepted()
    {
        BuyerAcceptanceItemStatus result = BuyerAcceptanceItemStatus.Parse("unknown-accepted");
        result.ShouldBe(BuyerAcceptanceItemStatus.UnknownAccepted);
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_Parse_Waived_ReturnsWaived()
    {
        BuyerAcceptanceItemStatus result = BuyerAcceptanceItemStatus.Parse("waived");
        result.ShouldBe(BuyerAcceptanceItemStatus.Waived);
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_Parse_UnknownValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => BuyerAcceptanceItemStatus.Parse("pending"));
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_SerializesAndDeserializesToCorrectValue()
    {
        foreach (BuyerAcceptanceItemStatus status in BuyerAcceptanceItemStatus.All)
        {
            string json = JsonSerializer.Serialize(status, WebOptions);
            json.ShouldBe($"\"{status.Value}\"");

            BuyerAcceptanceItemStatus? parsed = JsonSerializer.Deserialize<BuyerAcceptanceItemStatus>(json, WebOptions);
            parsed.ShouldNotBeNull();
            parsed.ShouldBe(status);
        }
    }

    [Fact]
    public void BuyerAcceptanceItemStatus_UnknownAccepted_WireValueIsUnknownAccepted()
    {
        string json = JsonSerializer.Serialize(BuyerAcceptanceItemStatus.UnknownAccepted, WebOptions);
        json.ShouldBe("\"unknown-accepted\"");
    }
}
