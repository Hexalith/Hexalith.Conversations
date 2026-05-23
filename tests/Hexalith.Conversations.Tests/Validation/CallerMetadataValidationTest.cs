// <copyright file="CallerMetadataValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Validation;

namespace Hexalith.Conversations.Tests.Validation;

/// <summary>
/// Verifies bounded, content-safe caller-metadata validation at the command boundary (Story 4.6).
/// Caller metadata is provenance only and never alters tenant scope, authorization, or trust state.
/// </summary>
public sealed class CallerMetadataValidationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-001");

    /// <summary>
    /// Ensures a command carrying valid bounded caller metadata passes envelope validation.
    /// </summary>
    [Fact]
    public void ValidCallerMetadataShouldPassEnvelopeValidation()
    {
        CallerMetadata caller = new(
            SchemaVersion.Current,
            "adopter-client",
            "1.4.0",
            "front-composer",
            "adopter-portal",
            "intake",
            new Dictionary<string, string> { ["channel"] = "web" });

        CreateConversationCommand command = new(Metadata(), CallerMetadata: caller);

        ConversationCommandSchemaValidation.ValidateEnvelope(command).ShouldBeNull();
    }

    /// <summary>
    /// Ensures a command with no caller metadata still passes (additive, optional).
    /// </summary>
    [Fact]
    public void MissingCallerMetadataShouldRemainValid()
        => ConversationCommandSchemaValidation.ValidateEnvelope(new CreateConversationCommand(Metadata())).ShouldBeNull();

    /// <summary>
    /// Ensures token-like caller-metadata values are rejected with a typed bounded diagnostic.
    /// </summary>
    [Theory]
    [InlineData("tenant:tenant-999")]
    [InlineData("party-hidden")]
    [InlineData("provider payload secret")]
    [InlineData("business reference case-123")]
    public void TokenLikeOrSensitiveCallerMetadataShouldBeRejectedAtConstruction(string sensitiveValue)
        => Should.Throw<ArgumentException>(() => new CallerMetadata(SchemaVersion.Current, Origin: sensitiveValue));

    /// <summary>
    /// Ensures the boundary validator rejects an oversized caller-metadata extension value with a typed diagnostic.
    /// Validation is exercised through the existing safe adopter metadata bag, which records construct freely but the
    /// boundary now bounds.
    /// </summary>
    [Fact]
    public void OversizedAttributesBagShouldReturnTypedRejection()
    {
        Dictionary<string, string> attributes = new()
        {
            ["k"] = new string('a', CallerMetadata.ValueMaxLength + 1),
        };

        UpdateConversationMetadataCommand command = new(Metadata(), Conversation, Attributes: attributes);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("caller_metadata_invalid");
    }

    /// <summary>
    /// Ensures the previously unbounded <see cref="UpdateConversationMetadataCommand.Attributes"/> bag now rejects
    /// content-unsafe values (tenant spoofing / token-like) at the command boundary with a typed diagnostic.
    /// </summary>
    [Fact]
    public void TenantSpoofingAttributesShouldReturnTypedRejection()
    {
        Dictionary<string, string> attributes = new()
        {
            ["tenantId"] = "tenant:tenant-999",
        };

        UpdateConversationMetadataCommand command = new(Metadata(), Conversation, Attributes: attributes);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("caller_metadata_invalid");
    }

    /// <summary>
    /// Ensures an over-count attributes bag returns a typed bounded rejection at the boundary.
    /// </summary>
    [Fact]
    public void TooManyAttributesShouldReturnTypedRejection()
    {
        Dictionary<string, string> attributes = new();
        for (int i = 0; i <= CallerMetadata.ExtensionEntryMaxCount; i++)
        {
            attributes[$"k{i}"] = "v";
        }

        UpdateConversationMetadataCommand command = new(Metadata(), Conversation, Attributes: attributes);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.Code.ShouldBe(ConversationErrorCode.CommandValidationFailed);
        rejection.ReasonCode.ShouldBe("caller_metadata_too_many_entries");
    }

    /// <summary>
    /// Ensures the diagnostic returned for invalid caller metadata is bounded and never echoes the caller value.
    /// </summary>
    [Fact]
    public void RejectionDiagnosticShouldNotEchoCallerSuppliedValue()
    {
        Dictionary<string, string> attributes = new()
        {
            ["secret"] = "tenant:tenant-999-super-secret",
        };

        UpdateConversationMetadataCommand command = new(Metadata(), Conversation, Attributes: attributes);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.ReasonCode.ShouldBe("caller_metadata_invalid");
        rejection.ReasonCode.ShouldNotContain("tenant-999");
        rejection.ReasonCode.ShouldNotContain("secret");
    }

    /// <summary>
    /// Ensures caller metadata is provenance only and cannot alter the tenant binding decided by the envelope.
    /// A caller claiming a different tenant in their metadata does not change the command's bound tenant.
    /// </summary>
    [Fact]
    public void CallerMetadataShouldNotAlterTenantBinding()
    {
        CallerMetadata caller = new(
            SchemaVersion.Current,
            "adopter-client",
            Origin: "adopter-portal",
            IntegrationContext: "elevated-origin");

        AppendMessageCommand command = new(Metadata(), Conversation, Message, Actor, "Hello", CallerMetadata: caller);

        // Caller metadata is provenance only; the bound tenant remains the envelope tenant, never a caller-supplied value.
        command.Metadata.TenantId.ShouldBe(Tenant);
        ConversationCommandSchemaValidation.ValidateEnvelope(command).ShouldBeNull();
    }

    /// <summary>
    /// Ensures valid bounded caller metadata also passes envelope validation on the append and update commands,
    /// proving the additive provenance parameter is bounded uniformly across every command that carries it.
    /// </summary>
    [Fact]
    public void ValidCallerMetadataShouldPassOnAppendAndUpdateCommands()
    {
        CallerMetadata caller = new(
            SchemaVersion.Current,
            "adopter-client",
            "1.4.0",
            "front-composer",
            "adopter-portal",
            "intake",
            new Dictionary<string, string> { ["channel"] = "web" });

        AppendMessageCommand append = new(Metadata(), Conversation, Message, Actor, "Hello", CallerMetadata: caller);
        UpdateConversationMetadataCommand update = new(Metadata(), Conversation, CallerMetadata: caller);

        ConversationCommandSchemaValidation.ValidateEnvelope(append).ShouldBeNull();
        ConversationCommandSchemaValidation.ValidateEnvelope(update).ShouldBeNull();
    }

    /// <summary>
    /// Ensures a caller-metadata rejection propagates the envelope correlation and causation identifiers so the
    /// typed diagnostic stays correlatable, while still never echoing any caller-supplied value.
    /// </summary>
    [Fact]
    public void CallerMetadataRejectionShouldPropagateCorrelationAndCausation()
    {
        Dictionary<string, string> attributes = new()
        {
            ["tenantId"] = "tenant:tenant-999",
        };

        UpdateConversationMetadataCommand command = new(Metadata(), Conversation, Attributes: attributes);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.CorrelationId.ShouldBe("correlation-001");
        rejection.CausationId.ShouldBe("causation-001");
        rejection.SchemaVersion.ShouldBe(SchemaVersion.Current);
    }

    /// <summary>
    /// Ensures the shared envelope gate runs before, and independently of, caller-metadata bounding: a command with an
    /// unsupported schema version is rejected on the envelope reason even when it carries valid caller metadata,
    /// proving caller metadata never substitutes for or relaxes the fail-closed envelope gates.
    /// </summary>
    [Fact]
    public void EnvelopeGateShouldRejectBeforeCallerMetadataIsConsidered()
    {
        ConversationCommandMetadata unsupportedSchema = new(
            new SchemaVersion(2),
            Tenant,
            Actor,
            "correlation-001",
            "causation-001",
            "idempotency-001");
        CallerMetadata caller = new(SchemaVersion.Current, "adopter-client");

        CreateConversationCommand command = new(unsupportedSchema, CallerMetadata: caller);

        ConversationRejectedDomainEvent? rejection = ConversationCommandSchemaValidation.ValidateEnvelope(command);

        rejection.ShouldNotBeNull();
        rejection.Code.ShouldBe(ConversationErrorCode.SchemaVersionUnsupported);
        rejection.ReasonCode.ShouldBe("unsupported_schema_version");
    }

    private static ConversationCommandMetadata Metadata()
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-001",
            "causation-001",
            "idempotency-001");
}
