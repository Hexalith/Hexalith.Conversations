// <copyright file="CapabilityReleaseScopeVocabularyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the <see cref="CapabilityReleaseScope"/> and <see cref="SubstrateConsequenceArea"/> closed vocabularies
/// for completeness, parse correctness, unknown-value rejection, and JSON round-trip.
/// </summary>
public sealed class CapabilityReleaseScopeVocabularyTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CapabilityReleaseScope_AllContains7Values()
    {
        CapabilityReleaseScope.All.Count.ShouldBe(7);
        string[] expected = ["v1", "v1-1", "vnext", "deferred", "waived", "conditional", "out-of-scope"];
        CapabilityReleaseScope.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void CapabilityReleaseScope_Parse_V1_ReturnsV1()
    {
        CapabilityReleaseScope result = CapabilityReleaseScope.Parse("v1");
        result.ShouldBe(CapabilityReleaseScope.V1);
    }

    [Fact]
    public void CapabilityReleaseScope_Parse_Deferred_ReturnsDeferred()
    {
        CapabilityReleaseScope result = CapabilityReleaseScope.Parse("deferred");
        result.ShouldBe(CapabilityReleaseScope.Deferred);
    }

    [Fact]
    public void CapabilityReleaseScope_Parse_UnknownValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => CapabilityReleaseScope.Parse("unknown-scope"));
    }

    [Fact]
    public void SubstrateConsequenceArea_AllContains8Values()
    {
        SubstrateConsequenceArea.All.Count.ShouldBe(8);
        string[] expected =
        [
            "tenant-isolation",
            "audit-pairing",
            "idempotency",
            "schema-evolution",
            "projection-freshness",
            "redaction-replay",
            "provider-portability",
            "adopter-compatibility",
        ];
        SubstrateConsequenceArea.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void SubstrateConsequenceArea_Parse_TenantIsolation_ReturnsTenantIsolation()
    {
        SubstrateConsequenceArea result = SubstrateConsequenceArea.Parse("tenant-isolation");
        result.ShouldBe(SubstrateConsequenceArea.TenantIsolation);
    }

    [Fact]
    public void SubstrateConsequenceArea_Parse_UnknownValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => SubstrateConsequenceArea.Parse("unknown-area"));
    }

    [Fact]
    public void CapabilityReleaseScope_SerializesAndDeserializesToCorrectValue()
    {
        string json = JsonSerializer.Serialize(CapabilityReleaseScope.Deferred, WebOptions);
        json.ShouldBe("\"deferred\"");

        CapabilityReleaseScope? parsed = JsonSerializer.Deserialize<CapabilityReleaseScope>("\"deferred\"", WebOptions);
        parsed.ShouldNotBeNull();
        parsed.ShouldBe(CapabilityReleaseScope.Deferred);
    }

    [Fact]
    public void SubstrateConsequenceArea_SerializesAndDeserializesToCorrectValue()
    {
        string json = JsonSerializer.Serialize(SubstrateConsequenceArea.AuditPairing, WebOptions);
        json.ShouldBe("\"audit-pairing\"");

        SubstrateConsequenceArea? parsed = JsonSerializer.Deserialize<SubstrateConsequenceArea>("\"audit-pairing\"", WebOptions);
        parsed.ShouldNotBeNull();
        parsed.ShouldBe(SubstrateConsequenceArea.AuditPairing);
    }
}
