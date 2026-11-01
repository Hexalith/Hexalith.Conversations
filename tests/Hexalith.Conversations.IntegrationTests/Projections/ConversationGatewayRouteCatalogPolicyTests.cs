// <copyright file="ConversationGatewayRouteCatalogPolicyTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>Preserves the accepted module idempotency meaning when the isolated live fixture declares its SDK admission facet.</summary>
public sealed class ConversationGatewayRouteCatalogPolicyTests
{
    /// <summary>Transport identities and clocks do not split equivalent Unicode-normalized creates.</summary>
    [Fact]
    public void EquivalentCreatesIgnoreEventIdentityTimestampsAndCorrelation()
    {
        CreateConversation first = Command("caf\u00e9", correlation: "first");
        CreateConversation retry = Command("cafe\u0301", correlation: "second") with
            { CreatedAt = first.CreatedAt.AddHours(1), EventId = "retry-event" };
        var original = Intent(first);
        var repeated = Intent(retry);

        repeated.CanonicalTarget.ShouldBe(original.CanonicalTarget);
        repeated.SemanticPayload.ShouldBe(original.SemanticPayload);
        ConversationGatewayRouteCatalog.CreateCommandAdapter().RetentionTier.ShouldBe(IdempotencyReplayRetentionTier.Mutation);
    }

    /// <summary>Business fields, identities, schema version and public scoped-key dimensions remain distinct.</summary>
    [Fact]
    public void ChangedBusinessMeaningOrScopeChangesTheSemanticIntent()
    {
        byte[] original = Intent(Command()).SemanticPayload;
        foreach (CreateConversation changed in new[]
        {
            Command(label: "changed"), Command(tenant: "tenant-beta"), Command(actor: "other-actor"),
            Command(conversation: "other-conversation"), Command(schema: 2), Command(key: "other-key"),
        })
        {
            Intent(changed).SemanticPayload.ShouldNotBe(original);
        }
    }

    /// <summary>An unallocated create fails instead of inventing an idempotency target.</summary>
    [Fact]
    public void UnallocatedCreateCannotDeclareIntent()
        => Should.Throw<InvalidOperationException>(() => Intent(Command() with { ConversationId = null }));

    private static CreateConversation Command(string label = "original", string tenant = "tenant-alpha", string actor = "actor-alpha",
        string conversation = "conversation-alpha", int schema = 1, string key = "key-alpha", string correlation = "correlation-alpha")
        => new(new CreateConversationCommand(new ConversationCommandMetadata(new SchemaVersion(schema), new TenantId(tenant),
                new PartyId(actor), correlation, "causation", key), Label: label),
            new ConversationId(conversation), DateTimeOffset.Parse("2026-11-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture), "original-event");

    private static IdempotencyCanonicalIntent Intent(CreateConversation command)
        => ConversationGatewayRouteCatalog.CreateCommandAdapter().CreateIntent(new IdempotencyIntentCommand(nameof(CreateConversation),
            "tenant-alpha", "conversation", "conversation-alpha", JsonSerializer.SerializeToUtf8Bytes(command), null));
}
