// <copyright file="ForbiddenPublicSurfaceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Contracts;
using Hexalith.Conversations.Contracts.Errors;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies public contracts do not expose infrastructure, personal-data, or unsafe provider details.
/// </summary>
public sealed class ForbiddenPublicSurfaceTest
{
    private static readonly string[] ForbiddenTerms =
    [
        "EventStore",
        "Envelope",
        "Snapshot",
        "Stream",
        "Sequence",
        "ExpectedRevision",
        "Checkpoint",
        "SignalR",
        "GroupName",
        "ProjectionTopology",
        "ProjectionName",
        "Handler",
        "Dispatcher",
        "Repository",
        "Store",
        "Subscription",
        "AggregateIdentity",
        "RawUpstream",
        "Token",
        "Claim",
        "Binary",
        "Payload",
        "Raw",
        "Email",
        "Phone",
        "Avatar",
        "DisplayName",
        "Contact",
        "Person",
        "Organization",
        "Exception",
    ];

    /// <summary>
    /// Ensures type and property names avoid forbidden public vocabulary.
    /// </summary>
    [Fact]
    public void PublicTypeAndPropertyNamesShouldAvoidForbiddenTerms()
    {
        Type[] exportedTypes = typeof(ContractsAssemblyMarker).Assembly.GetExportedTypes();

        foreach (Type type in exportedTypes)
        {
            AssertNoForbiddenTerms(type.FullName ?? type.Name);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                AssertNoForbiddenTerms(property.Name);
            }
        }
    }

    /// <summary>
    /// Ensures serialized JSON property names avoid forbidden vocabulary.
    /// </summary>
    [Fact]
    public void SerializedJsonShouldAvoidForbiddenTerms()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

        foreach (object sample in ContractSamples.AllContracts)
        {
            string json = JsonSerializer.Serialize(sample, sample.GetType(), options);
            foreach (string propertyName in GetJsonPropertyNames(json))
            {
                AssertNoForbiddenTerms(propertyName);
            }
        }
    }

    /// <summary>
    /// Ensures the word-boundary regex flags every forbidden term as a standalone name.
    /// </summary>
    [Fact]
    public void ForbiddenSurfaceRegexShouldFlagStandaloneTerms()
    {
        foreach (string term in ForbiddenTerms)
        {
            Should.Throw<Shouldly.ShouldAssertException>(() => AssertNoForbiddenTerms(term));
            Should.Throw<Shouldly.ShouldAssertException>(() => AssertNoForbiddenTerms($"foo_{term.ToLowerInvariant()}_bar"));
        }
    }

    /// <summary>
    /// Ensures the word-boundary regex does not over-match legitimate compound English words.
    /// </summary>
    [Theory]
    [InlineData("Upstream")]   // contains "Stream" without word boundary
    [InlineData("Personal")]   // contains "Person" without word boundary
    [InlineData("Stored")]     // contains "Store" without word boundary
    [InlineData("Streaming")]  // contains "Stream" without word boundary
    [InlineData("AggregateNotFound")]  // contains "Aggregate" but not "AggregateIdentity"
    [InlineData("non-existent")]
    [InlineData("PartyDirectory")]  // legitimate Conversations vocabulary
    public void ForbiddenSurfaceRegexShouldNotFlagLegitimateCompounds(string sample)
        => AssertNoForbiddenTerms(sample);

    /// <summary>
    /// Adversarially constructs <see cref="ConversationError"/> with each unsafe-term variant in
    /// every protected free-text field and asserts the contract refuses to build.
    /// </summary>
    [Fact]
    public void ConversationErrorShouldRejectUnsafeContentInEveryFreeTextField()
    {
        string[] unsafeSamples =
        [
            "EventStore envelope",
            "stream id 5",
            "snapshot at revision 42",
            "tenant-other-tenant",
            "provider-a session abc",
            "data store unavailable",
            "raw upstream payload",
            "dispatcher unavailable",
            "repository unreachable",
            "checkpoint 12345",
            "tenant:tenant-999",
            "tenant-999",
            "party:party-hidden",
            "party-hidden",
            "conv:conversation-hidden",
            "conversation-hidden",
            "provider-session-001",
            "provider payload body",
            "business reference case-123",
            "NullReferenceException at command boundary",
            "raw exception text",
            "C:\\private\\path",
            "D:\\secret\\path",
        ];

        foreach (string unsafeSample in unsafeSamples)
        {
            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                "correlation-001",
                DeveloperGuidance: unsafeSample));

            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                "correlation-001",
                AuditHandle: unsafeSample));

            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                unsafeSample));

            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                "correlation-001",
                SafeFieldDiagnostics: new Dictionary<string, string>
                {
                    ["target"] = unsafeSample,
                }));

            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                "correlation-001",
                SafeFieldDiagnostics: new Dictionary<string, string>
                {
                    [unsafeSample] = "value",
                }));

            Should.Throw<ArgumentException>(() => new ConversationError(
                ContractSamples.Version,
                ConversationErrorCode.IdempotencyConflict,
                ConversationErrorCategory.Conflict,
                true,
                "correlation-001",
                SafeMessage: unsafeSample));
        }
    }

    /// <summary>
    /// Curated <see cref="ContractSamples.SafeError"/> fixtures serialize without leaking unsafe terms.
    /// </summary>
    [Fact]
    public void CuratedFailClosedFixturesShouldRemainContentSafeOnTheWire()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        foreach (ConversationErrorCode code in ContractSamples.AllErrorCodes)
        {
            string json = JsonSerializer.Serialize(ContractSamples.SafeError(code), options);

            json.ShouldNotContain("other-tenant", Case.Insensitive);
            json.ShouldNotContain("redacted content", Case.Insensitive);
            json.ShouldNotContain("EventStore", Case.Insensitive);
            json.ShouldNotContain("raw upstream", Case.Insensitive);
            json.ShouldNotContain("aggregate identity", Case.Insensitive);
        }
    }

    private static void AssertNoForbiddenTerms(string value)
    {
        foreach (string term in ForbiddenTerms)
        {
            Regex.IsMatch(value, $@"(^|[^A-Za-z0-9]){Regex.Escape(term)}([^A-Za-z0-9]|$)", RegexOptions.IgnoreCase)
                .ShouldBeFalse($"Forbidden public term '{term}' found in '{value}'.");
        }
    }

    private static IEnumerable<string> GetJsonPropertyNames(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        foreach (string name in GetJsonPropertyNames(document.RootElement))
        {
            yield return name;
        }
    }

    private static IEnumerable<string> GetJsonPropertyNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                yield return property.Name;

                foreach (string childName in GetJsonPropertyNames(property.Value))
                {
                    yield return childName;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                foreach (string childName in GetJsonPropertyNames(child))
                {
                    yield return childName;
                }
            }
        }
    }
}
