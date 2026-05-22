// <copyright file="ConversationErrorCatalogTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies canonical typed error descriptors and safe remediation fields.
/// </summary>
public sealed class ConversationErrorCatalogTest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CatalogShouldDescribeEverySupportedErrorCode()
    {
        foreach (ConversationErrorCode code in ContractSamples.AllErrorCodes)
        {
            ConversationErrorDescriptor descriptor = ConversationErrorCatalog.Get(code);

            descriptor.Code.ShouldBe(code);
            descriptor.Category.ShouldNotBeNull();
            descriptor.IsRetryable.ShouldBe(ConversationErrorCode.IsRetryable(code));
            descriptor.ClientAction.ShouldNotBeNull();
            descriptor.SafeMessage.ShouldNotBeNullOrWhiteSpace();
            descriptor.Documentation.ShouldNotBeNull();
            descriptor.Documentation.IsAbsoluteUri.ShouldBeTrue();
            descriptor.Documentation.Scheme.ShouldBe(Uri.UriSchemeHttps);
        }

        ConversationErrorCatalog.All.Select(descriptor => descriptor.Code)
            .ShouldBe(ContractSamples.AllErrorCodes, ignoreOrder: true);
    }

    [Fact]
    public void CatalogShouldCreateSafeCanonicalErrorPayloads()
    {
        ConversationError error = ConversationErrorCatalog.CreateError(
            ConversationErrorCode.TenantIsolationViolation,
            "correlation-001",
            auditHandle: "audit-001",
            safeFieldDiagnostics: new Dictionary<string, string>
            {
                ["target"] = "hidden",
            },
            developerGuidance: "Check tenant access and caller authorization.");

        error.Category.ShouldBe(ConversationErrorCategory.Authorization);
        error.ClientAction.ShouldBe(ConversationErrorClientAction.CheckAccess);
        error.SafeMessage.ShouldBe("The request cannot be completed with the supplied access context.");
        error.AuditHandle.ShouldBeNull();
        error.DeveloperGuidance.ShouldBe("Check tenant access and caller authorization.");

        string json = JsonSerializer.Serialize(error, Options);
        json.ShouldContain("\"clientAction\":\"check-access\"");
        json.ShouldContain("\"safeMessage\":\"The request cannot be completed with the supplied access context.\"");
        json.ShouldNotContain("tenant-999", Case.Insensitive);
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("handler", Case.Insensitive);
        json.ShouldNotContain("D:\\", Case.Insensitive);
    }

    [Fact]
    public void CatalogShouldOnlyIncludeAuditHandleWhenDescriptorAllowsIt()
    {
        ConversationError allowed = ConversationErrorCatalog.CreateError(
            ConversationErrorCode.AuditPairingRequired,
            "correlation-001",
            auditHandle: "audit-001");
        ConversationError hidden = ConversationErrorCatalog.CreateError(
            ConversationErrorCode.AggregateNotFound,
            "correlation-001",
            auditHandle: "audit-001");

        allowed.AuditHandle.ShouldBe("audit-001");
        hidden.AuditHandle.ShouldBeNull();
    }

    [Fact]
    public void ClientActionShouldUseClosedVocabularySerialization()
    {
        JsonSerializer.Serialize(ConversationErrorClientAction.CorrectRequest, Options)
            .ShouldBe("\"correct-request\"");
        JsonSerializer.Deserialize<ConversationErrorClientAction>("\"correct-request\"", Options)
            .ShouldBe(ConversationErrorClientAction.CorrectRequest);

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ConversationErrorClientAction>("\"raw-server-debug\"", Options));
        Should.Throw<ArgumentException>(() => ConversationErrorClientAction.Parse("raw-server-debug"));
    }

    [Fact]
    public void ClosedVocabularyErrorParsingShouldNotEchoUnsupportedRawValues()
    {
        ArgumentException codeException = Should.Throw<ArgumentException>(
            () => ConversationErrorCode.Parse("tenant:tenant-999"));
        ArgumentException categoryException = Should.Throw<ArgumentException>(
            () => ConversationErrorCategory.Parse("tenant:tenant-999"));
        ArgumentException actionException = Should.Throw<ArgumentException>(
            () => ConversationErrorClientAction.Parse("tenant:tenant-999"));
        JsonException jsonException = Should.Throw<JsonException>(
            () => JsonSerializer.Deserialize<ConversationErrorClientAction>("\"tenant:tenant-999\"", Options));

        codeException.Message.ShouldNotContain("tenant-999", Case.Insensitive);
        categoryException.Message.ShouldNotContain("tenant-999", Case.Insensitive);
        actionException.Message.ShouldNotContain("tenant-999", Case.Insensitive);
        jsonException.Message.ShouldNotContain("tenant-999", Case.Insensitive);
        jsonException.InnerException.ShouldBeNull();
    }

    [Fact]
    public void AdditiveErrorFieldsShouldBeToleratedWhenAbsentOrUnknown()
    {
        string json = """
            {
              "schemaVersion": 1,
              "code": "command_validation_failed",
              "category": "validation",
              "isRetryable": false,
              "correlationId": "correlation-001",
              "documentation": "https://docs.hexalith.local/conversations/contracts/v1/errors",
              "unexpectedFutureField": "ignored"
            }
            """;

        ConversationError? error = JsonSerializer.Deserialize<ConversationError>(json, Options);

        error.ShouldNotBeNull();
        error.ClientAction.ShouldBeNull();
        error.SafeMessage.ShouldBeNull();
        error.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
    }
}
