// <copyright file="ForbiddenPublicSurfaceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;

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
            AssertNoForbiddenTerms(json);
        }
    }

    /// <summary>
    /// Ensures content-safe fail-closed errors do not disclose inaccessible target facts.
    /// </summary>
    [Fact]
    public void FailClosedErrorsShouldRemainContentSafe()
    {
        string[] failClosedCodes =
        [
            ConversationErrorCode.TenantIsolationViolation,
            ConversationErrorCode.AggregateNotFound,
            ConversationErrorCode.TenantProjectionStale,
            ConversationErrorCode.AuditSinkUnavailable,
            ConversationErrorCode.SchemaVersionUnsupported,
        ];

        foreach (string code in failClosedCodes)
        {
            string json = JsonSerializer.Serialize(ContractSamples.SafeError(code), new JsonSerializerOptions(JsonSerializerDefaults.Web));

            json.ShouldNotContain("other-tenant", Case.Insensitive);
            json.ShouldNotContain("exists", Case.Insensitive);
            json.ShouldNotContain("redacted content", Case.Insensitive);
            json.ShouldNotContain("provider-a", Case.Insensitive);
            json.ShouldNotContain("storage", Case.Insensitive);
        }
    }

    private static void AssertNoForbiddenTerms(string value)
    {
        foreach (string term in ForbiddenTerms)
        {
            value.ShouldNotContain(term, Case.Insensitive);
        }
    }
}
