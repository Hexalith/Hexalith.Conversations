// <copyright file="ConversationStateSafetyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.State;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.State;

/// <summary>
/// Verifies created conversation payloads remain tenant-safe and content-safe.
/// </summary>
public sealed class ConversationStateSafetyTest
{
    private static readonly string[] ForbiddenMemberTerms =
    [
        "DisplayName",
        "Email",
        "Phone",
        "PersonDetails",
        "OrganizationDetails",
        "Prompt",
        "ResponsePayload",
        "FileContent",
        "FileMetadata",
        "AccessToken",
        "Claim",
        "Authorization",
        "Stream",
        "Envelope",
        "Snapshot",
        "Sequence",
        "ExpectedRevision",
    ];

    /// <summary>
    /// Event and state members expose stable identifiers and metadata only.
    /// </summary>
    [Fact]
    public void CreateEventAndStateMembersShouldNotExposeForbiddenPayloadTerms()
    {
        Type[] inspectedTypes =
        [
            typeof(ConversationCreatedDomainEvent),
            typeof(ConversationRejectedDomainEvent),
            typeof(ConversationState),
        ];

        string[] memberNames = inspectedTypes
            .SelectMany(type => type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            .Select(member => member.Name)
            .ToArray();

        foreach (string forbidden in ForbiddenMemberTerms)
        {
            memberNames.ShouldNotContain(name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Serialized create event payloads do not contain personal, provider-content, or persistence mechanics fields.
    /// </summary>
    [Fact]
    public void SerializedCreateEventShouldNotContainForbiddenPayloadTerms()
    {
        ConversationCreatedDomainEvent created = ConversationAggregate
            .Handle(CreateCommand(), state: null)
            .Events
            .Single()
            .ShouldBeOfType<ConversationCreatedDomainEvent>();

        string json = JsonSerializer.Serialize(created);

        foreach (string forbidden in ForbiddenMemberTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }

        json.ShouldContain("tenant-safe");
        json.ShouldContain("conversation-safe");
        json.ShouldContain("party-safe");
        json.ShouldContain("provider-session-safe");
    }

    /// <summary>
    /// Serialized rejection event payloads also stay free of forbidden payload terms even when caller-supplied
    /// correlation and causation identifiers flow through.
    /// </summary>
    [Fact]
    public void SerializedRejectionEventShouldNotContainForbiddenPayloadTerms()
    {
        ConversationRejectedDomainEvent rejection = new(
            ConversationErrorCode.CommandValidationFailed,
            "command_validation_failed",
            SchemaVersion: SchemaVersion.Current,
            CorrelationId: "correlation-safe-rejection",
            CausationId: "causation-safe-rejection");

        string json = JsonSerializer.Serialize(rejection);

        foreach (string forbidden in ForbiddenMemberTerms)
        {
            json.ShouldNotContain(forbidden, Case.Insensitive);
        }

        json.ShouldContain("correlation-safe-rejection");
        json.ShouldContain("causation-safe-rejection");
        json.ShouldContain("command_validation_failed");
    }

    private static CreateConversation CreateCommand()
    {
        ConversationCommandMetadata metadata = new(
            SchemaVersion.Current,
            new TenantId("tenant-safe"),
            new PartyId("party-safe"),
            "correlation-safe",
            IdempotencyKey: "idempotency-safe");

        CreateConversationCommand publicCommand = new(
            metadata,
            BusinessReference: new BusinessReference("crm", "case-safe"),
            ProviderCorrelation: new ProviderCorrelationMetadata(
                "contoso-ai",
                "assistant",
                SchemaVersion.Current,
                ProviderSessionReference: "provider-session-safe"));

        return new CreateConversation(
            publicCommand,
            new ConversationId("conversation-safe"),
            new DateTimeOffset(2026, 5, 18, 14, 0, 0, TimeSpan.Zero),
            "event-safe");
    }
}
