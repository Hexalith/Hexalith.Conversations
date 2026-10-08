using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Actual ordered-delivery adapter/pump against persisted serialized Conversation replay; receiver/current enrollment remain synthetic.</summary>
public sealed class ConversationDeletionPublicationDeliveryTests
{
    private static async Task<ConversationDeletionDeliveryPumpFixture> ApprovedAsync()
    {
        var source = new F(); var before = await source.ReplayAsync();
        source.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(source.CommandMetadata(F.Human, "approval"),
            F.Conversation, "independent-approval", before.SourceRevision, F.At.AddMinutes(1), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
        var stream = (await source.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken)).Stream!;
        return new(source, new(1, new ConversationDeletionPublicationProjector().Project(stream, TestContext.Current.CancellationToken).Single()));
    }
    /// <summary>Only persisted source receipt confirms an acknowledged offset; accepted/unpersisted, unknown, malformed and absent enrollment do not advance.</summary>
    [Theory]
    [InlineData("available", SourcePublicationDeliveryStatus.Acknowledged)]
    [InlineData("unpersisted", SourcePublicationDeliveryStatus.Unavailable)]
    [InlineData("unknown", SourcePublicationDeliveryStatus.Unavailable)]
    [InlineData("malformed", SourcePublicationDeliveryStatus.Quarantined)]
    [InlineData("missing-enrollment", SourcePublicationDeliveryStatus.Unavailable)]
    public async Task OnlyExactPersistedOriginalAcknowledgementAdvances(string vector, SourcePublicationDeliveryStatus expected)
    {
        var f = await ApprovedAsync();
        f.PersistCommands = vector != "unpersisted"; f.UnknownLookup = vector == "unknown"; f.ChangedReceipt = vector == "malformed";
        f.BadBinding = vector == "missing-enrollment" ? "missing" : null;
        (await new ConversationDeletionPublicationDelivery(f.Pump()).DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(expected);
        var durable = (await f.Source.ReplayAsync()).DeletionSource;
        if (expected == SourcePublicationDeliveryStatus.Acknowledged) { durable.AcknowledgedSourceRevision.ShouldBe(f.Entry.Publication.SourceRevision); durable.Acknowledgement.ShouldNotBeNull(); }
        else { durable.AcknowledgedSourceRevision.ShouldBe(0); }
    }
    /// <summary>Original receiver/source acknowledgement survives lost response, process restart and target rollover with no repeated physical effect.</summary>
    [Fact]
    public async Task RestartAndTargetRolloverRecoverSameOriginalSourceAck()
    {
        var f = await ApprovedAsync(); f.LoseReceiverResponse = true; var original = f.Entry.Publication;
        (await new ConversationDeletionPublicationDelivery(f.Pump()).DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Unavailable);
        f.Target = "receiver-v2";
        (await new ConversationDeletionPublicationDelivery(f.Pump()).DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Acknowledged);
        var source = (await f.Source.ReplayAsync()).DeletionSource;
        source.Signal!.ConversationDeletionSignalId.ShouldBe(original.PublicationId); source.Signal.SourceRevision.ShouldBe(original.SourceRevision);
        source.TargetVersion.ShouldBe("receiver-v1"); f.Submissions.ShouldBe(1); f.AttemptCommands.Count.ShouldBe(1);
        (await new ConversationDeletionPublicationDelivery(f.Pump()).DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Acknowledged);
        f.Submissions.ShouldBe(1);
    }
}
