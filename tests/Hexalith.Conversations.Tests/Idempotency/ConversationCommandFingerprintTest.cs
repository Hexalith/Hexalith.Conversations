// <copyright file="ConversationCommandFingerprintTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Idempotency;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Idempotency;

/// <summary>
/// Verifies the Conversations-owned idempotency scope and canonical fingerprint contract.
/// </summary>
public sealed class ConversationCommandFingerprintTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly PartyId Participant = new("party-participant");
    private static readonly MessageId Message = new("message-001");
    private static readonly FileId File = new("file-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly BusinessReference Business = new("crm", "case-123");

    /// <summary>
    /// P50 review fix (2026-05-20): two fingerprints built from independently-constructed metadata with identical
    /// (tenant, command type, conversation scope, idempotency key, schema version, canonical payload) tuples must
    /// produce equal scope AND equal payload-fingerprint values. AC1 "stable outcome" depends on this positive
    /// equivalence and the existing tests only assert negatives.
    /// </summary>
    [Fact]
    public void EquivalentMetadataShouldProduceIdenticalFingerprint()
    {
        // Build the same logical command twice from independently-constructed metadata instances.
        ConversationCommandFingerprint createA = ConversationCommandFingerprint.Create(CreateConversation(), Conversation);
        ConversationCommandFingerprint createB = ConversationCommandFingerprint.Create(CreateConversation(), Conversation);

        ConversationCommandFingerprint appendA = ConversationCommandFingerprint.Create(
            new AppendMessageCommand(Metadata(), Conversation, Message, Actor, "Hello", Provider("session-a")),
            Conversation);
        ConversationCommandFingerprint appendB = ConversationCommandFingerprint.Create(
            new AppendMessageCommand(Metadata(), Conversation, Message, Actor, "Hello", Provider("session-b")),
            Conversation);

        ConversationCommandFingerprint addParticipantA = ConversationCommandFingerprint.Create(
            new AddParticipantCommand(Metadata(), Conversation, Participant, ParticipantType.Human, ParticipantRole.Member, Provider("session-a")),
            Conversation);
        ConversationCommandFingerprint addParticipantB = ConversationCommandFingerprint.Create(
            new AddParticipantCommand(Metadata(), Conversation, Participant, ParticipantType.Human, ParticipantRole.Member, Provider("session-b")),
            Conversation);
        ConversationCommandFingerprint retentionA = ConversationCommandFingerprint.Create(RetentionCommand(), Conversation);
        ConversationCommandFingerprint retentionB = ConversationCommandFingerprint.Create(RetentionCommand(), Conversation);

        createA.Scope.ShouldBe(createB.Scope);
        createA.PayloadFingerprint.ShouldBe(createB.PayloadFingerprint);

        appendA.Scope.ShouldBe(appendB.Scope);
        appendA.PayloadFingerprint.ShouldBe(appendB.PayloadFingerprint);

        addParticipantA.Scope.ShouldBe(addParticipantB.Scope);
        addParticipantA.PayloadFingerprint.ShouldBe(addParticipantB.PayloadFingerprint);

        retentionA.Scope.ShouldBe(retentionB.Scope);
        retentionA.PayloadFingerprint.ShouldBe(retentionB.PayloadFingerprint);
    }

    /// <summary>
    /// Provider correlation changes do not affect the canonical fingerprint because provider IDs are not authority.
    /// </summary>
    [Fact]
    public void ProviderCorrelationChangesShouldNotChangeFingerprint()
    {
        CreateConversationCommand first = CreateConversation(providerSession: "provider-session-a");
        CreateConversationCommand second = CreateConversation(providerSession: "provider-session-b");

        ConversationCommandFingerprint firstFingerprint = ConversationCommandFingerprint.Create(first, Conversation);
        ConversationCommandFingerprint secondFingerprint = ConversationCommandFingerprint.Create(second, Conversation);

        firstFingerprint.Scope.ShouldBe(secondFingerprint.Scope);
        firstFingerprint.PayloadFingerprint.ShouldBe(secondFingerprint.PayloadFingerprint);
        firstFingerprint.ToString().ShouldNotContain("provider-session-a", Case.Insensitive);
        firstFingerprint.ToString().ShouldNotContain("provider-session-b", Case.Insensitive);
    }

    /// <summary>
    /// Safe command meaning changes do affect the fingerprint.
    /// </summary>
    [Fact]
    public void DifferentSafePayloadMeaningShouldChangeFingerprint()
    {
        ConversationCommandFingerprint first = ConversationCommandFingerprint.Create(
            CreateConversation(label: "Case 123"),
            Conversation);
        ConversationCommandFingerprint second = ConversationCommandFingerprint.Create(
            CreateConversation(label: "Case 456"),
            Conversation);

        first.Scope.ShouldBe(second.Scope);
        first.PayloadFingerprint.ShouldNotBe(second.PayloadFingerprint);
    }

    /// <summary>
    /// Every public command type in the story matrix receives a scoped idempotency key.
    /// </summary>
    [Fact]
    public void CommandMatrixShouldProduceExpectedScopes()
    {
        (object Command, ConversationCommandType CommandType, string ScopeKind)[] commands =
        [
            (CreateConversation(), ConversationCommandType.CreateConversationCommand, ConversationIdempotencyScope.CreateAllocationScopeKind),
            (new AppendMessageCommand(Metadata(), Conversation, Message, Actor, "Hello", Provider("session")),
                ConversationCommandType.AppendMessageCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (new AddParticipantCommand(Metadata(), Conversation, Participant, ParticipantType.Human, ParticipantRole.Member, Provider("session")),
                ConversationCommandType.AddParticipantCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (new AttachFileReferenceCommand(Metadata(), Conversation, File, Folder, Message),
                ConversationCommandType.AttachFileReferenceCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (new UpdateConversationMetadataCommand(Metadata(), Conversation, "Case 123", Business),
                ConversationCommandType.UpdateConversationMetadataCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (new CloseConversationCommand(Metadata(), Conversation, "resolved"),
                ConversationCommandType.CloseConversationCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (new ArchiveConversationCommand(Metadata(), Conversation, "retained"),
                ConversationCommandType.ArchiveConversationCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (RetentionCommand(),
                ConversationCommandType.SetConversationRetentionPolicyCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
            (RedactionCommand(),
                ConversationCommandType.RedactMessageContentCommand,
                ConversationIdempotencyScope.ConversationScopeKind),
        ];

        foreach ((object command, ConversationCommandType commandType, string scopeKind) in commands)
        {
            ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command, Conversation);

            fingerprint.Scope.TenantId.ShouldBe(Tenant);
            fingerprint.Scope.CommandType.ShouldBe(commandType);
            fingerprint.Scope.ScopeKind.ShouldBe(scopeKind);
            fingerprint.Scope.ScopeValue.ShouldBe(Conversation.Value);
            fingerprint.Scope.IdempotencyKey.ShouldBe("idempotency-001");
            fingerprint.Scope.SchemaVersion.ShouldBe(SchemaVersion.Current);
        }
    }

    /// <summary>
    /// Dictionary order and null/empty safe metadata are canonicalized without weakening identity fields.
    /// </summary>
    [Fact]
    public void SafeMetadataOrderingShouldBeCanonicalized()
    {
        UpdateConversationMetadataCommand first = new(
            Metadata(),
            Conversation,
            "Case 123",
            Business,
            new Dictionary<string, string>
            {
                ["priority"] = "normal",
                ["owner"] = "support",
            });

        UpdateConversationMetadataCommand second = new(
            Metadata(),
            Conversation,
            "Case 123",
            Business,
            new Dictionary<string, string>
            {
                ["owner"] = "support",
                ["priority"] = "normal",
            });

        ConversationCommandFingerprint nullAttributes = ConversationCommandFingerprint.Create(
            new UpdateConversationMetadataCommand(Metadata(), Conversation, "Case 123", Business),
            Conversation);
        ConversationCommandFingerprint emptyAttributes = ConversationCommandFingerprint.Create(
            new UpdateConversationMetadataCommand(Metadata(), Conversation, "Case 123", Business, new Dictionary<string, string>()),
            Conversation);

        ConversationCommandFingerprint.Create(first, Conversation).PayloadFingerprint
            .ShouldBe(ConversationCommandFingerprint.Create(second, Conversation).PayloadFingerprint);
        nullAttributes.PayloadFingerprint.ShouldBe(emptyAttributes.PayloadFingerprint);
    }

    /// <summary>
    /// P46: Safe payload text uses NFC canonicalization without changing identity dimensions.
    /// </summary>
    [Fact]
    public void SafeTextShouldUseNfcNormalization()
    {
        ConversationCommandFingerprint composedLabel = ConversationCommandFingerprint.Create(
            CreateConversation(label: "Caf\u00e9"),
            Conversation);
        ConversationCommandFingerprint decomposedLabel = ConversationCommandFingerprint.Create(
            CreateConversation(label: "Cafe\u0301"),
            Conversation);
        ConversationCommandFingerprint composedAttribute = ConversationCommandFingerprint.Create(
            UpdateMetadata(attributes: new Dictionary<string, string> { ["note"] = "Caf\u00e9" }),
            Conversation);
        ConversationCommandFingerprint decomposedAttribute = ConversationCommandFingerprint.Create(
            UpdateMetadata(attributes: new Dictionary<string, string> { ["note"] = "Cafe\u0301" }),
            Conversation);

        composedLabel.PayloadFingerprint.ShouldBe(decomposedLabel.PayloadFingerprint);
        composedAttribute.PayloadFingerprint.ShouldBe(decomposedAttribute.PayloadFingerprint);
    }

    /// <summary>
    /// Scope comparison remains exact and does not collapse tenant, command, schema, or conversation identity.
    /// </summary>
    [Fact]
    public void ScopeIdentityShouldNotUseLossyNormalization()
    {
        ConversationCommandFingerprint baseline = ConversationCommandFingerprint.Create(CreateConversation(), Conversation);
        ConversationCommandFingerprint tenantCaseChange = ConversationCommandFingerprint.Create(
            CreateConversation(metadata: Metadata(tenant: new TenantId("TENANT-001"))),
            Conversation);
        ConversationCommandFingerprint differentConversation = ConversationCommandFingerprint.Create(
            CreateConversation(),
            new ConversationId("conversation-002"));
        ConversationCommandFingerprint differentSchema = ConversationCommandFingerprint.Create(
            CreateConversation(metadata: Metadata(schemaVersion: new SchemaVersion(SchemaVersion.Current.Value + 1))),
            Conversation);
        ConversationCommandFingerprint differentCommandType = ConversationCommandFingerprint.Create(
            new CloseConversationCommand(Metadata(), Conversation, "resolved"),
            Conversation);
        ConversationCommandFingerprint visuallySimilarTenant = ConversationCommandFingerprint.Create(
            CreateConversation(metadata: Metadata(tenant: new TenantId("tenant-\u043e01"))),
            Conversation);
        ConversationCommandFingerprint nfkcEquivalentTenant = ConversationCommandFingerprint.Create(
            CreateConversation(metadata: Metadata(tenant: new TenantId("\uff54enant-001"))),
            Conversation);

        baseline.Scope.ShouldNotBe(tenantCaseChange.Scope);
        baseline.Scope.ShouldNotBe(differentConversation.Scope);
        baseline.Scope.ShouldNotBe(differentSchema.Scope);
        baseline.Scope.ShouldNotBe(differentCommandType.Scope);
        baseline.Scope.ShouldNotBe(visuallySimilarTenant.Scope);
        baseline.Scope.ShouldNotBe(nfkcEquivalentTenant.Scope);
    }

    /// <summary>
    /// P39: Missing required canonical fields fail eagerly with a clear canonical field name.
    /// </summary>
    [Fact]
    public void MissingRequiredCanonicalFieldShouldThrowClearArgumentException()
    {
        ArgumentException exception = Should.Throw<ArgumentException>(() => ConversationCommandFingerprint.Create(
            new AppendMessageCommand(Metadata(), Conversation, Message, Actor, " "),
            Conversation));

        // P47 review fix (2026-05-20): ParamName now surfaces the canonical field name, not the local 'value' parameter.
        exception.ParamName.ShouldBe("text");
        exception.Message.ShouldContain("canonical field 'text'");
    }

    private static CreateConversationCommand CreateConversation(
        ConversationCommandMetadata? metadata = null,
        string label = "Case 123",
        string providerSession = "provider-session")
        => new(metadata ?? Metadata(), Business, new ProjectId("project-001"), Folder, label, Provider(providerSession));

    private static UpdateConversationMetadataCommand UpdateMetadata(
        ConversationCommandMetadata? metadata = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        => new(
            metadata ?? Metadata(),
            Conversation,
            "Case 123",
            new BusinessReference("crm", "case-123"),
            attributes);

    private static SetConversationRetentionPolicyCommand RetentionCommand()
        => new(
            Metadata(),
            Conversation,
            "retention-policy-standard",
            "customer-request",
            new DateTimeOffset(2026, 5, 19, 9, 0, 0, TimeSpan.Zero));

    private static RedactMessageContentCommand RedactionCommand()
        => new(
            Metadata(),
            Conversation,
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            new DateTimeOffset(2026, 5, 19, 9, 5, 0, TimeSpan.Zero));

    private static ConversationCommandMetadata Metadata(
        TenantId? tenant = null,
        SchemaVersion? schemaVersion = null)
        => new(
            schemaVersion ?? SchemaVersion.Current,
            tenant ?? Tenant,
            Actor,
            "correlation-001",
            "causation-001",
            "idempotency-001");

    private static ProviderCorrelationMetadata Provider(string providerSession)
        => new(
            "provider-a",
            "assistant",
            SchemaVersion.Current,
            providerSession,
            "provider-response",
            new Dictionary<string, string> { ["thread"] = "thread-001" });
}
