// <copyright file="GenericValueConverterSkeletonTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Characterizes the token-type guards of the two genuinely-ruleless generic value-converter base
/// skeletons (Story 2.6 / FR-8): <c>ConversationStringValueJsonConverter&lt;T&gt;</c> and
/// <c>ConversationIntValueJsonConverter&lt;T&gt;</c>. These skeletons are the named deletion target for the
/// FR-14 / Story 3.6 shared-helper replacement; pinning their malformed-token rejection here gives that
/// future replacement a behavior-exact oracle to preserve. The skeletons are exercised through the public
/// value types whose converters derive from them (<see cref="ProjectionTrustState"/>,
/// <see cref="ProjectionFreshnessReasonCode"/>, <see cref="SchemaVersion"/>) — no source under
/// <c>src/Serialization</c> is modified, and the existing positive wire-shape oracle is left intact.
/// </summary>
public sealed class GenericValueConverterSkeletonTest
{
    /// <summary>
    /// The string-value base skeleton accepts only a JSON string token; a number, object, array, or boolean
    /// token must be rejected as a <see cref="JsonException"/> before any domain parse runs. Top-level JSON
    /// <c>null</c> is short-circuited by System.Text.Json to a C# null and is asserted separately.
    /// </summary>
    [Theory]
    [InlineData("123")] // number token
    [InlineData("1.5")] // fractional number token
    [InlineData("true")] // boolean token
    [InlineData("false")] // boolean token
    [InlineData("{}")] // object token
    [InlineData("[]")] // array token
    [InlineData("[\"Current\"]")] // array wrapping a would-be-valid value
    public void StringValueSkeletonShouldRejectNonStringTokens(string json)
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectionTrustState>(json));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProjectionFreshnessReasonCode>(json));
    }

    /// <summary>
    /// The int-value base skeleton accepts only a JSON integer token that fits <see cref="int"/>; a non-number
    /// token, or a number that overflows <see cref="int"/>, must be rejected as a <see cref="JsonException"/>
    /// (the <c>TryGetInt32</c> guard) rather than surfacing a raw overflow or producing a truncated value.
    /// </summary>
    [Theory]
    [InlineData("true")] // boolean token
    [InlineData("{}")] // object token
    [InlineData("[]")] // array token
    [InlineData("[1]")] // array wrapping a would-be-valid value
    [InlineData("\"1\"")] // string-wrapped integer
    [InlineData("2147483648")] // int.MaxValue + 1 (Int32 overflow)
    [InlineData("9999999999")] // well beyond Int32 range
    [InlineData("-2147483649")] // int.MinValue - 1 (Int32 underflow)
    public void IntValueSkeletonShouldRejectNonInt32Tokens(string json)
        => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>(json));

    /// <summary>
    /// Confirms the skeletons stay symmetric on the happy path: a canonical value still round-trips to itself,
    /// so the new negative-path assertions above are characterizing rejection without disturbing acceptance.
    /// </summary>
    [Fact]
    public void SkeletonsShouldStillRoundTripCanonicalValues()
    {
        JsonSerializer.Deserialize<ProjectionTrustState>(
            JsonSerializer.Serialize(ProjectionTrustState.Current)).ShouldBe(ProjectionTrustState.Current);
        JsonSerializer.Deserialize<ProjectionFreshnessReasonCode>(
            JsonSerializer.Serialize(ProjectionFreshnessReasonCode.Current)).ShouldBe(ProjectionFreshnessReasonCode.Current);
        JsonSerializer.Deserialize<SchemaVersion>(
            JsonSerializer.Serialize(SchemaVersion.Current)).ShouldBe(SchemaVersion.Current);
    }
}
