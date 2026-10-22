// <copyright file="ConversationAdmissionSafeguardTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;
using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Verifies authenticated legacy actors and portable restricted occurrence evidence.</summary>
public sealed class ConversationAdmissionSafeguardTests
{
    private static readonly string[] LegacyNames = [nameof(CreateConversation), nameof(AddParticipant),
        nameof(ReassignConversationProject), nameof(SetConversationRetentionPolicy),
        nameof(MarkConversationContentSensitive), nameof(RedactMessageContent)];

    /// <summary>Gets every legacy wrapper, supported alias and authenticated actor or scope combination.</summary>
    public static TheoryData<string, string, string> LegacyScopeCases
    {
        get
        {
            var cases = new TheoryData<string, string, string>();
            foreach (string command in LegacyNames)
            {
                foreach (string alias in new[] { "short", "full", "assembly-qualified" })
                {
                    foreach (string scope in new[] { "exact", "wrong-actor", "wrong-tenant", "wrong-conversation" })
                    {
                        cases.Add(command, alias, scope);
                    }
                }
            }
            return cases;
        }
    }

    /// <summary>Gets malformed payload and unsupported alias combinations for every legacy wrapper.</summary>
    public static TheoryData<string, string> InvalidLegacyCases
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (string command in LegacyNames)
            {
                foreach (string mutation in new[] { "null", "malformed-json", "null-public-command",
                    "wrong-namespace", "wrong-assembly", "qualified-short-name", "unknown-command" })
                {
                    cases.Add(command, mutation);
                }
            }
            return cases;
        }
    }

    /// <summary>General grants bind the exact human actor and payload scope for every supported alias.</summary>
    /// <param name="commandName">The registered legacy wrapper.</param>
    /// <param name="alias">The supported discriminator representation.</param>
    /// <param name="scope">The exact scope or one independently altered field.</param>
    [Theory]
    [MemberData(nameof(LegacyScopeCases))]
    public async Task LegacyCommandsRequireExactAuthenticatedActorAndScope(string commandName, string alias, string scope)
    {
        var fixture = new F();
        var metadata = new ConversationCommandMetadata(SchemaVersion.Current,
            scope == "wrong-tenant" ? new TenantId("foreign-tenant") : F.Tenant,
            scope == "wrong-actor" ? F.Agent : F.Human, "legacy-correlation", IdempotencyKey: "legacy-intent");
        ConversationId conversation = scope == "wrong-conversation" ? new("foreign-conversation") : F.Conversation;
        object command = LegacyCommand(commandName, metadata, conversation);
        Type type = command.GetType();
        CommandEnvelope envelope = fixture.Envelope(command, "human") with
        {
            CommandType = alias switch
            {
                "short" => type.Name,
                "full" => type.FullName!,
                "assembly-qualified" => type.AssemblyQualifiedName!,
                _ => throw new ArgumentOutOfRangeException(nameof(alias)),
            },
        };

        var result = await fixture.Admission.EvaluateAsync(new(new(envelope, null)), TestContext.Current.CancellationToken);

        result.IsRejected.ShouldBe(scope != "exact");
        fixture.Reads.ShouldBe(0);
        fixture.PersistedEvents.Count.ShouldBe(1);
    }

    /// <summary>Malformed legacy payloads and unsupported aliases cannot inherit a general human grant.</summary>
    /// <param name="commandName">The registered legacy wrapper.</param>
    /// <param name="mutation">The payload or discriminator defect.</param>
    [Theory]
    [MemberData(nameof(InvalidLegacyCases))]
    public async Task InvalidLegacyPayloadsAndAliasesNeverReachProcessing(string commandName, string mutation)
    {
        var fixture = new F();
        object command = LegacyCommand(commandName, fixture.CommandMetadata(F.Human), F.Conversation);
        Type type = command.GetType();
        CommandEnvelope envelope = fixture.Envelope(command, "human") with { CommandType = type.Name };
        envelope = mutation switch
        {
            "null" => envelope with { Payload = Encoding.UTF8.GetBytes("null") },
            "malformed-json" => envelope with { Payload = Encoding.UTF8.GetBytes("{") },
            "null-public-command" => envelope with { Payload = Encoding.UTF8.GetBytes("{\"publicCommand\":null}") },
            "wrong-namespace" => envelope with { CommandType = $"Untrusted.Commands.{type.Name}" },
            "wrong-assembly" => envelope with { CommandType = $"{type.FullName}, Untrusted.Commands" },
            "qualified-short-name" => envelope with { CommandType = $"{type.Name}, {type.Assembly.GetName().Name}" },
            "unknown-command" => envelope with { CommandType = "UnknownConversationMutation" },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var result = await fixture.Admission.EvaluateAsync(new(new(envelope, null)), TestContext.Current.CancellationToken);

        result.IsRejected.ShouldBeTrue();
        fixture.AuthorityCalls.ShouldBe(0);
        fixture.Reads.ShouldBe(0);
        fixture.PersistedEvents.Count.ShouldBe(1);
    }

    /// <summary>General command admission refuses incomplete, mismatched or organizational authority responses.</summary>
    /// <param name="mutation">The missing or contradictory authenticated authority field.</param>
    [Theory]
    [InlineData("missing-party")]
    [InlineData("missing-revision")]
    [InlineData("wrong-principal")]
    [InlineData("wrong-tenant")]
    [InlineData("organization-identity")]
    public async Task GeneralCommandRequiresCompleteMatchingHumanAuthority(string mutation)
    {
        var fixture = new F
        {
            TransformAuthority = authorization => mutation switch
            {
                "missing-party" => authorization with { PartyId = null },
                "missing-revision" => authorization with { AuthorityRevision = null },
                "wrong-principal" => authorization with { PrincipalId = "foreign-principal" },
                "wrong-tenant" => authorization with { TenantId = new("foreign-tenant") },
                "organization-identity" => authorization with { OrganizationIdentityConfirmed = true },
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            },
        };
        var command = new AddParticipant(new(fixture.CommandMetadata(F.Human), F.Conversation, F.Agent,
            ParticipantType.AiAgent, ParticipantRole.Member), F.At.AddMinutes(1), "legacy-authority-event");

        var result = await fixture.Admission.EvaluateAsync(new(new(fixture.Envelope(command, "human"), null)),
            TestContext.Current.CancellationToken);

        result.IsRejected.ShouldBeTrue();
        fixture.AuthorityCalls.ShouldBe(1);
        fixture.Reads.ShouldBe(0);
        fixture.PersistedEvents.Count.ShouldBe(1);
    }

    /// <summary>Registered SDK routing rejects forged actors and preserves the authenticated actor on persisted membership.</summary>
    /// <param name="alias">The supported discriminator representation.</param>
    [Theory]
    [InlineData("short")]
    [InlineData("full")]
    [InlineData("assembly-qualified")]
    public async Task RegisteredLegacyDispatchRejectsForgedActorAndPreservesHumanAttribution(string alias)
    {
        var fixture = new F();
        var current = await fixture.CurrentStateAsync();
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        var services = builder.Services;
        services.AddSingleton<IConversationCommandSourceVerifier>(fixture);
        services.AddSingleton<IConversationAgentAuthority>(fixture);
        services.AddSingleton<IConversationDeletionApprovalVerifier>(fixture);
        services.AddSingleton<IConversationDeletionReceiptVerifier>(fixture);
        services.AddSingleton<IConversationTenantCatalogue>(fixture);
        services.AddSingleton<Hexalith.EventStore.Client.Streams.IAuthoritativeEventStreamReader>(fixture);
        services.AddConversationTenantAccess();
        services.AddConversationQueries(options => options.MaxOffset = 100_000);
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<IDomainProcessor>("conversation").ShouldBeOfType<ConversationAggregate>();
        string discriminator = alias switch
        {
            "short" => typeof(AddParticipant).Name,
            "full" => typeof(AddParticipant).FullName!,
            "assembly-qualified" => typeof(AddParticipant).AssemblyQualifiedName!,
            _ => throw new ArgumentOutOfRangeException(nameof(alias)),
        };
        var forged = new AddParticipant(new(fixture.CommandMetadata(F.Agent), F.Conversation, F.Agent,
            ParticipantType.AiAgent, ParticipantRole.Member), F.At.AddMinutes(1), "legacy-dispatch-event");
        var forgedEnvelope = fixture.Envelope(forged, "human") with { CommandType = discriminator };

        var refused = await DomainServiceRequestRouter.ProcessAsync(scope.ServiceProvider, new(forgedEnvelope, current),
            cancellationToken: TestContext.Current.CancellationToken);

        refused.IsRejection.ShouldBeTrue();
        var rejected = refused.Events.Single();
        rejected.EventTypeName.ShouldEndWith(nameof(ConversationRejectedDomainEvent));
        JsonSerializer.Deserialize<ConversationRejectedDomainEvent>(rejected.Payload, F.Options)!.ReasonCode
            .ShouldBe($"agents-{ConversationAgentsOutcome.Denied}");
        fixture.PersistedEvents.Count.ShouldBe(1);

        var valid = forged with { PublicCommand = forged.PublicCommand with { Metadata = fixture.CommandMetadata(F.Human) } };
        var accepted = await DomainServiceRequestRouter.ProcessAsync(scope.ServiceProvider,
            new(fixture.Envelope(valid, "human") with { CommandType = discriminator }, current),
            cancellationToken: TestContext.Current.CancellationToken);

        accepted.IsRejection.ShouldBeFalse();
        var acceptedWire = accepted.Events.Single();
        acceptedWire.EventTypeName.ShouldEndWith(nameof(ParticipantAddedDomainEvent));
        var added = JsonSerializer.Deserialize<ParticipantAddedDomainEvent>(acceptedWire.Payload, F.Options)!;
        added.Metadata.ActorPartyId.ShouldBe(F.Human);
        added.Metadata.TenantId.ShouldBe(F.Tenant);
        added.Metadata.ConversationId.ShouldBe(F.Conversation);
        added.ParticipantPartyId.ShouldBe(F.Agent);
        fixture.Persist(DomainResult.Success([added]));
        (await fixture.ReplayAsync()).Participants.Single().PartyId.ShouldBe(F.Agent);
        fixture.PersistedEvents.Count.ShouldBe(2);
    }

    /// <summary>Membership timestamps are required and bound before both initial append and persisted retry.</summary>
    /// <param name="mutation">The missing or contradictory occurrence evidence.</param>
    [Theory]
    [InlineData("missing-portable")]
    [InlineData("portable-mismatch")]
    [InlineData("wrapper-mismatch")]
    public async Task MembershipRequiresBoundTimestampsBeforeAppendAndRetry(string mutation)
    {
        var fixture = new F();
        AddAgentParticipant valid = fixture.Membership;
        var (portable, wrapper) = MutatedTimestamps(mutation, valid.AddedAt);
        AddAgentParticipant invalid = valid with
        {
            PublicCommand = valid.PublicCommand with { OperationTimestamp = portable },
            AddedAt = wrapper,
        };
        var initial = await fixture.ReplayAsync();

        AssertInvalid(ConversationAggregate.Handle(invalid, initial));
        var accepted = ConversationAggregate.Handle(valid, initial);
        accepted.IsSuccess.ShouldBeTrue();
        accepted.Events.Count.ShouldBe(1);
        fixture.Persist(accepted);
        var persisted = await fixture.ReplayAsync();

        var retry = ConversationAggregate.Handle(valid, persisted);
        retry.IsRejection.ShouldBeFalse();
        retry.Events.ShouldBeEmpty();
        AssertInvalid(ConversationAggregate.Handle(invalid, persisted));
        (await fixture.ReplayAsync()).Participants.Count.ShouldBe(1);
        fixture.PersistedEvents.Count.ShouldBe(2);
    }

    /// <summary>Posting timestamps remain required and bound when a message is already persisted.</summary>
    /// <param name="mutation">The missing or contradictory occurrence evidence.</param>
    [Theory]
    [InlineData("missing-portable")]
    [InlineData("portable-mismatch")]
    [InlineData("wrapper-mismatch")]
    public async Task PostingRequiresBoundTimestampsBeforeAppendAndRetry(string mutation)
    {
        var fixture = new F();
        fixture.Persist(ConversationAggregate.Handle(fixture.Membership, await fixture.ReplayAsync()));
        AppendAgentMessage valid = fixture.Posting;
        var (portable, wrapper) = MutatedTimestamps(mutation, valid.PostedAt);
        AppendAgentMessage invalid = valid with
        {
            PublicCommand = valid.PublicCommand with { OperationTimestamp = portable },
            PostedAt = wrapper,
        };
        var initial = await fixture.ReplayAsync();

        AssertInvalid(ConversationAggregate.Handle(invalid, initial));
        var accepted = ConversationAggregate.Handle(valid, initial);
        accepted.IsSuccess.ShouldBeTrue();
        accepted.Events.Count.ShouldBe(1);
        fixture.Persist(accepted);
        var persisted = await fixture.ReplayAsync();

        var retry = ConversationAggregate.Handle(valid, persisted);
        retry.IsRejection.ShouldBeFalse();
        retry.Events.ShouldBeEmpty();
        AssertInvalid(ConversationAggregate.Handle(invalid, persisted));
        (await fixture.ReplayAsync()).Messages.Count.ShouldBe(1);
        fixture.PersistedEvents.Count.ShouldBe(3);
    }

    /// <summary>Equivalent membership instants preserve supported offsets through append and serialized retry.</summary>
    /// <param name="portableOffsetHours">The portable timestamp offset from UTC.</param>
    /// <param name="wrapperOffsetHours">The wrapper timestamp offset from UTC.</param>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(1, -2)]
    public async Task MembershipAcceptsEquivalentInstantsWithDifferentOffsets(int portableOffsetHours, int wrapperOffsetHours)
    {
        var fixture = new F();
        AddAgentParticipant original = fixture.Membership;
        AddAgentParticipant command = original with
        {
            PublicCommand = original.PublicCommand with
            {
                OperationTimestamp = original.AddedAt.ToOffset(TimeSpan.FromHours(portableOffsetHours)),
            },
            AddedAt = original.AddedAt.ToOffset(TimeSpan.FromHours(wrapperOffsetHours)),
        };

        var accepted = ConversationAggregate.Handle(command, await fixture.ReplayAsync());

        accepted.IsSuccess.ShouldBeTrue();
        accepted.Events.Count.ShouldBe(1);
        fixture.Persist(accepted);
        var retry = ConversationAggregate.Handle(command, await fixture.ReplayAsync());
        retry.IsRejection.ShouldBeFalse();
        retry.Events.ShouldBeEmpty();
        (await fixture.ReplayAsync()).Participants.Count.ShouldBe(1);
        fixture.PersistedEvents.Count.ShouldBe(2);
    }

    /// <summary>Equivalent posting instants preserve supported offsets through append and serialized retry.</summary>
    /// <param name="portableOffsetHours">The portable timestamp offset from UTC.</param>
    /// <param name="wrapperOffsetHours">The wrapper timestamp offset from UTC.</param>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(1, -2)]
    public async Task PostingAcceptsEquivalentInstantsWithDifferentOffsets(int portableOffsetHours, int wrapperOffsetHours)
    {
        var fixture = new F();
        fixture.Persist(ConversationAggregate.Handle(fixture.Membership, await fixture.ReplayAsync()));
        AppendAgentMessage original = fixture.Posting;
        AppendAgentMessage command = original with
        {
            PublicCommand = original.PublicCommand with
            {
                OperationTimestamp = original.PostedAt.ToOffset(TimeSpan.FromHours(portableOffsetHours)),
            },
            PostedAt = original.PostedAt.ToOffset(TimeSpan.FromHours(wrapperOffsetHours)),
        };

        var accepted = ConversationAggregate.Handle(command, await fixture.ReplayAsync());

        accepted.IsSuccess.ShouldBeTrue();
        accepted.Events.Count.ShouldBe(1);
        fixture.Persist(accepted);
        var retry = ConversationAggregate.Handle(command, await fixture.ReplayAsync());
        retry.IsRejection.ShouldBeFalse();
        retry.Events.ShouldBeEmpty();
        (await fixture.ReplayAsync()).Messages.Count.ShouldBe(1);
        fixture.PersistedEvents.Count.ShouldBe(3);
    }

    /// <summary>An exact original post retry survives a later human edit without restoring original content.</summary>
    [Fact]
    public async Task ExactPostingRetryAfterLaterHumanEditPreservesEditedContentAndProvenance()
    {
        var fixture = new F();
        fixture.Persist(ConversationAggregate.Handle(fixture.Membership, await fixture.ReplayAsync()));
        fixture.Persist(ConversationAggregate.Handle(fixture.Posting, await fixture.ReplayAsync()));
        var edit = new EditConversationMessageCommand(fixture.CommandMetadata(F.Human), F.Conversation,
            fixture.Posting.PublicCommand.MessageId, "Authenticated later edit", F.At.AddMinutes(3));
        var current = await fixture.CurrentStateAsync();
        var admitted = await fixture.Admission.EvaluateAsync(new(new(fixture.Envelope(edit, "human"), current)),
            TestContext.Current.CancellationToken);
        admitted.IsRejected.ShouldBeFalse();
        var changed = ConversationAggregate.Handle(edit, await fixture.ReplayAsync());
        changed.IsSuccess.ShouldBeTrue();
        fixture.Persist(changed);

        var retry = ConversationAggregate.Handle(fixture.Posting, await fixture.ReplayAsync());

        retry.IsRejection.ShouldBeFalse();
        retry.Events.ShouldBeEmpty();
        var message = (await fixture.ReplayAsync()).Messages.Single();
        message.Text.ShouldBe(edit.Text);
        message.OriginalText.ShouldBe(fixture.Posting.PublicCommand.Text);
        message.Provenance!.HumanEdited.ShouldBeTrue();
        message.Provenance.EditedByPartyId.ShouldBe(F.Human);
        fixture.PersistedEvents.Count.ShouldBe(4);
    }

    private static object LegacyCommand(string name, ConversationCommandMetadata metadata, ConversationId conversation)
    {
        var audit = new GovernanceAuditEvidenceReference(new AuditEvidenceHandle("legacy-audit"), "legacy-policy", F.At.AddMinutes(1));
        return name switch
        {
            nameof(CreateConversation) => new CreateConversation(new(metadata), conversation, F.At, "legacy-event"),
            nameof(AddParticipant) => new AddParticipant(new(metadata, conversation, F.Agent,
                ParticipantType.AiAgent, ParticipantRole.Member), F.At.AddMinutes(1), "legacy-event"),
            nameof(ReassignConversationProject) => new ReassignConversationProject(new(metadata, conversation,
                new(ConversationProjectAssignmentOperation.Clear)), F.At.AddMinutes(1), "legacy-event"),
            nameof(SetConversationRetentionPolicy) => new SetConversationRetentionPolicy(new(metadata, conversation,
                "legacy-policy", "legacy-rationale", F.At.AddMinutes(1)), audit, "legacy-event"),
            nameof(MarkConversationContentSensitive) => new MarkConversationContentSensitive(new(metadata, conversation,
                new(GovernedTargetKind.Conversation), SensitivityCategory.Restricted, "legacy-policy", "legacy-rationale",
                F.At.AddMinutes(1)), audit, "legacy-event"),
            nameof(RedactMessageContent) => new RedactMessageContent(new(metadata, conversation,
                new(GovernedTargetKind.Message, MessageId: new("message-original")), RedactionCategory.ContentSuppression,
                "legacy-policy", "legacy-rationale", F.At.AddMinutes(1)), audit, "legacy-event"),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    private static (DateTimeOffset? Portable, DateTimeOffset Wrapper) MutatedTimestamps(string mutation, DateTimeOffset valid)
        => mutation switch
        {
            "missing-portable" => (null, valid),
            "portable-mismatch" => (valid.AddSeconds(1), valid),
            "wrapper-mismatch" => (valid, valid.AddSeconds(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

    private static void AssertInvalid(DomainResult result)
    {
        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ConversationRejectedDomainEvent>().ReasonCode
            .ShouldBe($"agents-{ConversationAgentsOutcome.Invalid}");
    }
}
