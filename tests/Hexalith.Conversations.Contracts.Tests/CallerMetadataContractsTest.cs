// <copyright file="CallerMetadataContractsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies the bounded, content-safe caller-metadata provenance contract (Story 4.6).
/// </summary>
public sealed class CallerMetadataContractsTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensures valid caller metadata is accepted and the approved provenance fields are preserved.
    /// </summary>
    [Fact]
    public void ValidCallerMetadataShouldBeAccepted()
    {
        CallerMetadata metadata = new(
            SchemaVersion.Current,
            "adopter-client",
            "1.4.0",
            "front-composer",
            "adopter-portal",
            "intake",
            new Dictionary<string, string> { ["channel"] = "web" });

        metadata.ClientName.ShouldBe("adopter-client");
        metadata.ClientVersion.ShouldBe("1.4.0");
        metadata.ComposerSource.ShouldBe("front-composer");
        metadata.Origin.ShouldBe("adopter-portal");
        metadata.IntegrationContext.ShouldBe("intake");
        metadata.ExtensionData.ShouldNotBeNull();
        metadata.ExtensionData!["channel"].ShouldBe("web");
        CallerMetadata.TryValidateBounds(metadata, out string? reason).ShouldBeTrue();
        reason.ShouldBeNull();
    }

    /// <summary>
    /// Ensures caller metadata serializes to a stable camelCase web JSON shape and round-trips.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldKeepStableCamelCaseJsonShape()
    {
        string expected =
            """
            {"metadataSchemaVersion":1,"clientName":"adopter-client","clientVersion":"1.4.0","composerSource":"front-composer","origin":"adopter-portal","integrationContext":"intake","extensionData":{"channel":"web"}}
            """;

        JsonNode? expectedNode = JsonNode.Parse(expected);
        JsonNode? actualNode = JsonNode.Parse(JsonSerializer.Serialize(ContractSamples.Caller, WebOptions));

        JsonNode.DeepEquals(actualNode, expectedNode).ShouldBeTrue(JsonSerializer.Serialize(ContractSamples.Caller, WebOptions));
    }

    /// <summary>
    /// Ensures caller metadata tolerates additive (unknown) JSON members.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldTolerateAdditiveJson()
    {
        string json =
            """
            {"metadataSchemaVersion":1,"clientName":"adopter-client","futureField":"ignored","extensionData":{"channel":"web"}}
            """;

        CallerMetadata? metadata = JsonSerializer.Deserialize<CallerMetadata>(json, WebOptions);

        metadata.ShouldNotBeNull();
        metadata!.ClientName.ShouldBe("adopter-client");
    }

    /// <summary>
    /// Ensures caller metadata round-trips through web-default JSON preserving every approved provenance field,
    /// not just the first member exercised by the additive-JSON tolerance test.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRoundTripPreservingEveryField()
    {
        string json = JsonSerializer.Serialize(ContractSamples.Caller, WebOptions);

        CallerMetadata? metadata = JsonSerializer.Deserialize<CallerMetadata>(json, WebOptions);

        metadata.ShouldNotBeNull();
        metadata!.MetadataSchemaVersion.ShouldBe(SchemaVersion.Current);
        metadata.ClientName.ShouldBe("adopter-client");
        metadata.ClientVersion.ShouldBe("1.4.0");
        metadata.ComposerSource.ShouldBe("front-composer");
        metadata.Origin.ShouldBe("adopter-portal");
        metadata.IntegrationContext.ShouldBe("intake");
        metadata.ExtensionData.ShouldNotBeNull();
        metadata.ExtensionData!["channel"].ShouldBe("web");
        CallerMetadata.TryValidateBounds(metadata, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures a minimal caller metadata instance (schema version only, no optional fields) is accepted and
    /// round-trips. Provenance is entirely optional beyond the required schema version.
    /// </summary>
    [Fact]
    public void MinimalCallerMetadataShouldBeAcceptedAndRoundTrip()
    {
        CallerMetadata metadata = new(SchemaVersion.Current);

        metadata.ClientName.ShouldBeNull();
        metadata.ExtensionData.ShouldBeNull();
        CallerMetadata.TryValidateBounds(metadata, out string? reason).ShouldBeTrue();
        reason.ShouldBeNull();

        CallerMetadata? roundTripped = JsonSerializer.Deserialize<CallerMetadata>(
            JsonSerializer.Serialize(metadata, WebOptions),
            WebOptions);

        roundTripped.ShouldNotBeNull();
        roundTripped!.MetadataSchemaVersion.ShouldBe(SchemaVersion.Current);
        roundTripped.ClientName.ShouldBeNull();
    }

    /// <summary>
    /// Ensures an empty (zero-entry) extension bag is accepted: the count cap bounds the upper edge only.
    /// </summary>
    [Fact]
    public void EmptyExtensionDataShouldBeAccepted()
    {
        CallerMetadata metadata = new(
            SchemaVersion.Current,
            "adopter-client",
            ExtensionData: new Dictionary<string, string>());

        metadata.ExtensionData.ShouldNotBeNull();
        metadata.ExtensionData!.Count.ShouldBe(0);
        CallerMetadata.TryValidateBounds(metadata, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures the extension count cap is an inclusive boundary: exactly the maximum number of entries is accepted.
    /// </summary>
    [Fact]
    public void ExtensionDataAtExactCountCapShouldBeAccepted()
    {
        Dictionary<string, string> exactlyMax = new();
        for (int i = 0; i < CallerMetadata.ExtensionEntryMaxCount; i++)
        {
            exactlyMax[$"k{i}"] = "v";
        }

        CallerMetadata metadata = new(SchemaVersion.Current, ExtensionData: exactlyMax);

        metadata.ExtensionData!.Count.ShouldBe(CallerMetadata.ExtensionEntryMaxCount);
        CallerMetadata.TryValidateBounds(metadata, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures the per-field length cap is an inclusive boundary: a value exactly at the maximum length is accepted.
    /// </summary>
    [Fact]
    public void FieldAtExactLengthCapShouldBeAccepted()
    {
        string atCap = new('a', CallerMetadata.ValueMaxLength);

        CallerMetadata metadata = new(SchemaVersion.Current, ClientName: atCap);

        metadata.ClientName!.Length.ShouldBe(CallerMetadata.ValueMaxLength);
        CallerMetadata.TryValidateBounds(metadata, out _).ShouldBeTrue();
    }

    /// <summary>
    /// Ensures the construction-time content-safety guardrail rejects additional authority-claim and infrastructure
    /// fragments supplied as caller metadata, proving caller metadata cannot smuggle protected material into provenance.
    /// </summary>
    [Theory]
    [InlineData("provider response body")]
    [InlineData("conv:conversation-001")]
    [InlineData("aggregate identity leaked")]
    [InlineData("other-tenant scope")]
    [InlineData("raw upstream body")]
    public void CallerMetadataShouldRejectAdditionalSensitiveFragments(string sensitiveValue)
        => Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, ComposerSource: sensitiveValue));

    /// <summary>
    /// Ensures caller metadata rejects values exceeding the per-field length cap.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRejectOversizedField()
    {
        string oversized = new('a', CallerMetadata.ValueMaxLength + 1);

        Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, ClientName: oversized));
    }

    /// <summary>
    /// Ensures caller metadata rejects an oversized extension value.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRejectOversizedExtensionValue()
    {
        string oversized = new('a', CallerMetadata.ValueMaxLength + 1);

        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string> { ["k"] = oversized }));
    }

    /// <summary>
    /// Ensures caller metadata rejects more extension entries than the bounded count cap.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRejectTooManyExtensionEntries()
    {
        Dictionary<string, string> tooMany = new();
        for (int i = 0; i <= CallerMetadata.ExtensionEntryMaxCount; i++)
        {
            tooMany[$"k{i}"] = "v";
        }

        Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, ExtensionData: tooMany));
    }

    /// <summary>
    /// Ensures caller metadata rejects control characters in fields and extension entries.
    /// </summary>
    [Theory]
    [InlineData("line\nbreak")]
    [InlineData("tab\tvalue")]
    [InlineData("null\0byte")]
    public void CallerMetadataShouldRejectControlCharacters(string value)
    {
        Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, Origin: value));
        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string> { ["k"] = value }));
    }

    /// <summary>
    /// Ensures caller metadata rejects content-unsafe fragments (tenant/Party/provider payload/secret/path/exception).
    /// </summary>
    [Theory]
    [InlineData("tenant:tenant-999")]
    [InlineData("party-hidden")]
    [InlineData("provider payload body")]
    [InlineData("business reference case-123")]
    [InlineData("EventStore stream")]
    [InlineData("C:\\private\\path")]
    [InlineData("D:\\secret\\path")]
    [InlineData("NullReferenceException at boundary")]
    public void CallerMetadataShouldRejectContentUnsafeValues(string unsafeValue)
    {
        Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, ClientName: unsafeValue));
        Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, Origin: unsafeValue));
        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string> { ["k"] = unsafeValue }));
        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string> { [unsafeValue] = "v" }));
    }

    /// <summary>
    /// Ensures whitespace-only fields are rejected while a null (absent) field remains allowed.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("")]
    public void CallerMetadataShouldRejectWhitespaceOnlyFields(string value)
        => Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, ClientName: value));

    /// <summary>
    /// Ensures caller metadata rejects malformed extension data (empty key or null value).
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRejectMalformedExtensionData()
    {
        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string> { [" "] = "value" }));

        Should.Throw<ArgumentException>(() => new CallerMetadata(
            SchemaVersion.Current,
            ExtensionData: new Dictionary<string, string?> { ["channel"] = null }! as IReadOnlyDictionary<string, string>));
    }

    /// <summary>
    /// Ensures the schema version is required.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldRequireSchemaVersion()
        => Should.Throw<ArgumentNullException>(() => new CallerMetadata(null!));

    /// <summary>
    /// Ensures the boundary bag bounding helper rejects oversized, control-character, and content-unsafe entries,
    /// covering the previously unbounded <c>UpdateConversationMetadataCommand.Attributes</c> bag.
    /// </summary>
    [Fact]
    public void TryValidateMetadataBagShouldRejectUnsafeAttributes()
    {
        CallerMetadata.TryValidateMetadataBag(
            new Dictionary<string, string> { ["priority"] = "normal" },
            out string? safeReason).ShouldBeTrue();
        safeReason.ShouldBeNull();

        CallerMetadata.TryValidateMetadataBag(
            new Dictionary<string, string> { ["k"] = new string('a', CallerMetadata.ValueMaxLength + 1) },
            out string? oversizedReason).ShouldBeFalse();
        oversizedReason.ShouldBe("caller_metadata_invalid");

        CallerMetadata.TryValidateMetadataBag(
            new Dictionary<string, string> { ["tenant"] = "tenant:tenant-999" },
            out string? unsafeReason).ShouldBeFalse();
        unsafeReason.ShouldBe("caller_metadata_invalid");

        Dictionary<string, string> tooMany = new();
        for (int i = 0; i <= CallerMetadata.ExtensionEntryMaxCount; i++)
        {
            tooMany[$"k{i}"] = "v";
        }

        CallerMetadata.TryValidateMetadataBag(tooMany, out string? countReason).ShouldBeFalse();
        countReason.ShouldBe("caller_metadata_too_many_entries");
    }
}
