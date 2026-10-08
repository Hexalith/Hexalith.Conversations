using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Actual approval-event projection with persisted replay simulation, never production authority.</summary>
public sealed class ConversationDeletionPublicationProjectorTests
{
    private static async Task<(F Fixture, AuthoritativeEventStream Source)> Approved()
    {
        var f = new F(); f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        f.Persist(ConversationAggregate.Handle(f.Posting, await f.ReplayAsync()));
        var before = await f.ReplayAsync();
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"), F.Conversation,
            "independent-approval", before.SourceRevision, F.At.AddMinutes(3), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(3))), "approval-event");
        f.Persist(ConversationAggregate.Handle(approval, before));
        return (f, (await f.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken)).Stream!);
    }

    /// <summary>Existing approval append is the publication intent; replay emits only its original safe descriptor.</summary>
    [Fact]
    public async Task SourceAtomicApprovalProducesStableDescriptorWithoutContent()
    {
        var (f, source) = await Approved(); var projector = new ConversationDeletionPublicationProjector();
        var descriptor = projector.Project(source, TestContext.Current.CancellationToken).Single();
        var approval = f.PersistedEvents.OfType<ConversationDeletionApprovedDomainEvent>().Single();
        descriptor.PublicationId.ShouldBe(approval.Signal.ConversationDeletionSignalId);
        descriptor.SourceRevision.ShouldBe(4); descriptor.SourceMessageId.ShouldBe("persisted-4");
        descriptor.Identity.ShouldBe(source.Identity); descriptor.StableFieldsDigest.Length.ShouldBe(64);
        var json = JsonSerializer.Serialize(descriptor); json.ShouldNotContain("Original content"); json.ShouldNotContain("immutable-organization-agent");
        projector.Project(source, TestContext.Current.CancellationToken).Single().ShouldBe(descriptor);
    }

    /// <summary>Malformed or altered same-source logical publication never becomes a discovery descriptor.</summary>
    [Theory]
    [InlineData("identity")]
    [InlineData("revision")]
    [InlineData("version")]
    public async Task AlteredLogicalPublicationCannotBeProjected(string vector)
    {
        var (_, source) = await Approved();
        var approval = JsonSerializer.Deserialize<ConversationDeletionApprovedDomainEvent>(source.Events[^1].Payload, F.Options)!;
        var signal = vector switch { "identity" => approval.Signal with { ConversationDeletionSignalId = "changed-same-source-identity" },
            "revision" => approval.Signal with { SourceRevision = 3 }, _ => approval.Signal with { SourceContractVersion = 2 } };
        var records = source.Events.ToArray(); records[^1] = records[^1] with { Payload = JsonSerializer.SerializeToUtf8Bytes(approval with { Signal = signal }, F.Options) };
        Should.Throw<InvalidOperationException>(() => new ConversationDeletionPublicationProjector().Project(source with { Events = records }, TestContext.Current.CancellationToken));
    }

    /// <summary>Creation/content without an approved decision yields no fabricated deletion publication.</summary>
    [Fact]
    public async Task UnapprovedConversationHasNoPublication()
    {
        var f = new F(); var source = (await f.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken)).Stream!;
        new ConversationDeletionPublicationProjector().Project(source, TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    /// <summary>The original caller token stops replay/projection before any descriptor release.</summary>
    [Fact]
    public async Task CancelledProjectionPreservesOriginalToken()
    {
        var (_, source) = await Approved(); using var caller = new CancellationTokenSource(); caller.Cancel();
        var exception = Should.Throw<OperationCanceledException>(() => new ConversationDeletionPublicationProjector().Project(source, caller.Token));
        exception.CancellationToken.ShouldBe(caller.Token);
    }
}
