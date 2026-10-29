// <copyright file="CompleteSourceConversationCommandSourceVerifierTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Security.Cryptography;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Maps only the SDK-admitted complete source; synthetic native proofs, never a production platform binding.</summary>
public sealed class CompleteSourceConversationCommandSourceVerifierTests
{
    private const string Incarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private static readonly AggregateIdentity Source = new(F.Tenant.Value, "conversation", F.Conversation.Value);
    private readonly FixedClock _clock = new(F.At.AddHours(1).AddSeconds(30));

    /// <summary>An exact admitted prefix becomes an authoritative stream with its native proof, head and events, and admission accepts it.</summary>
    [Fact]
    public async Task CompleteAdmittedSourceIsAuthoritativeAndAdmitsTheCommand()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        CompleteCommandSource source = await CompleteAsync(f);
        var verifier = new CompleteSourceConversationCommandSourceVerifier(_clock, () => source);
        var request = new DomainServiceRequest(f.Envelope(f.Membership), await f.CurrentStateAsync());

        AuthoritativeStreamReadResult result = await verifier.VerifyAsync(request, TestContext.Current.CancellationToken);

        result.IsAuthoritative.ShouldBeTrue(result.FailureReason);
        AuthoritativeEventStream stream = result.Stream!;
        stream.Identity.ShouldBe(Source);
        stream.Head.ShouldBe(source.Events.Count);
        stream.NativeProof.ShouldBe(source.Proof);
        stream.ObservedAt.ShouldBe(source.ObservedAt);
        stream.ObservationId.ShouldNotBeNullOrWhiteSpace();
        stream.Events.Select(static e => e.MessageId).ShouldBe(source.Events.Select(static e => e.MessageId));
        stream.Events[0].Payload.ShouldNotBeSameAs(source.Events[0].Payload, "the mapped prefix owns its bytes");

        var admission = new ConversationAgentAdmissionStage(f, f, f, verifier);
        (await admission.EvaluateAsync(new(request), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
    }

    /// <summary>An absent, foreign, expired, gapped, erased or mismatched prefix is not authoritative, with an exact reason.</summary>
    [Theory]
    [InlineData("absent", "complete-source-absent")]
    [InlineData("foreign-tenant", "complete-source-foreign")]
    [InlineData("foreign-aggregate", "complete-source-foreign")]
    [InlineData("non-conversation-domain", "complete-source-foreign")]
    [InlineData("null-command", "complete-source-request-invalid")]
    [InlineData("expired", "complete-source-expired")]
    [InlineData("future", "complete-source-expired")]
    [InlineData("valid-until-not-after-observed", "complete-source-expired")]
    [InlineData("default-observed", "complete-source-expired")]
    [InlineData("gap", "complete-source-gap")]
    [InlineData("over-limit", "complete-source-gap")]
    [InlineData("null-payload", "complete-source-gap")]
    [InlineData("erased", "complete-source-erased")]
    [InlineData("head-mismatch", "complete-source-proof-mismatch")]
    [InlineData("digest-mismatch", "complete-source-proof-mismatch")]
    [InlineData("head-message-mismatch", "complete-source-proof-mismatch")]
    [InlineData("missing-digest", "complete-source-proof-mismatch")]
    [InlineData("caller-state-proof", "complete-source-caller-proof-refused")]
    [InlineData("caller-effective-events", "complete-source-caller-proof-refused")]
    public async Task UnusableSourceIsUnavailable(string fault, string reason)
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        CompleteCommandSource complete = await CompleteAsync(f);
        DateTimeOffset now = _clock.GetUtcNow();
        CompleteCommandSource? source = fault switch
        {
            "absent" => null,
            "foreign-tenant" => complete with { Identity = new AggregateIdentity("tenant-beta", "conversation", F.Conversation.Value) },
            "foreign-aggregate" => complete with { Identity = new AggregateIdentity(F.Tenant.Value, "conversation", "conversation-beta") },
            "non-conversation-domain" => complete with { Identity = new AggregateIdentity(F.Tenant.Value, "tenants", F.Conversation.Value) },
            "expired" => complete with { ValidUntil = now },
            "future" => complete with { ObservedAt = now.AddSeconds(1), ValidUntil = now.AddMinutes(1) },
            "valid-until-not-after-observed" => complete with { ObservedAt = now.AddSeconds(-5), ValidUntil = now.AddSeconds(-5) },
            "default-observed" => complete with { ObservedAt = default },
            "gap" => complete with { Events = [complete.Events[0] with { SequenceNumber = 2 }, complete.Events[1]] },
            "over-limit" => complete with { Events = [.. Enumerable.Repeat(complete.Events[0], CompleteCommandSource.MaximumEvents + 1)] },
            "null-payload" => complete with { Events = [complete.Events[0], complete.Events[1] with { Payload = null! }] },
            "erased" => complete with { Proof = complete.Proof with { Erased = true } },
            "head-mismatch" => complete with { Proof = complete.Proof with { HeadSequence = 1 } },
            "digest-mismatch" => complete with { Events = [complete.Events[0], complete.Events[1] with { NativeEventDigest = new string('0', 64) }] },
            "head-message-mismatch" => complete with { Proof = complete.Proof with { HeadMessageId = "persisted-1" } },
            "missing-digest" => complete with { Events = [complete.Events[0], complete.Events[1] with { NativeEventDigest = null }] },
            "null-command" or "caller-state-proof" or "caller-effective-events" => complete,
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };
        CommandEnvelope envelope = f.Envelope(f.Membership);
        if (fault == "non-conversation-domain")
        {
            envelope = envelope with { Domain = "tenants" };
        }

        var verifier = new CompleteSourceConversationCommandSourceVerifier(_clock, () => source);
        var request = new DomainServiceRequest(fault == "null-command" ? null! : envelope, await f.CurrentStateAsync())
        {
            CommandStateProof = fault == "caller-state-proof" ? [1, 2, 3] : null,
            VerifiedEffectiveEvents = fault == "caller-effective-events" ? [] : null,
        };

        AuthoritativeStreamReadResult result = await verifier.VerifyAsync(request, TestContext.Current.CancellationToken);

        result.Stream.ShouldBeNull(fault);
        result.FailureReason.ShouldBe(reason, fault);
        if (fault is not ("null-command" or "non-conversation-domain"))
        {
            // Admission accepts other domains without Conversation checks and cannot evaluate a missing command.
            var admission = new ConversationAgentAdmissionStage(f, f, f, verifier);
            (await admission.EvaluateAsync(new(request), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue(fault);
        }
    }

    /// <summary>An explicit empty incarnation is a complete authoritative prefix at head zero.</summary>
    [Fact]
    public async Task EmptyPrefixIsAuthoritative()
    {
        var f = new F();
        DateTimeOffset now = _clock.GetUtcNow();
        var empty = new CompleteCommandSource(Source, "platform-authority-1", now.AddSeconds(-10), now.AddMinutes(2),
            NativeSourceProofCodec.CreateSeed(Source, Incarnation), []);
        var verifier = new CompleteSourceConversationCommandSourceVerifier(_clock, () => empty);

        AuthoritativeStreamReadResult result = await verifier.VerifyAsync(new DomainServiceRequest(f.Envelope(f.Membership), null),
            TestContext.Current.CancellationToken);

        result.IsAuthoritative.ShouldBeTrue(result.FailureReason);
        result.Stream!.Head.ShouldBe(0);
        result.Stream.Events.ShouldBeEmpty();
        result.Stream.NativeProof.ShouldNotBeNull().HeadMessageId.ShouldBeNull();
    }

    /// <summary>The production constructor reads only the SDK request-local admitted source, never the caller-supplied one.</summary>
    [Fact]
    public async Task ProductionConstructorReadsOnlyTheSdkAdmittedContext()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        CompleteCommandSource source = await CompleteAsync(f);
        var verifier = new CompleteSourceConversationCommandSourceVerifier(_clock);
        var request = new DomainServiceRequest(f.Envelope(f.Membership), await f.CurrentStateAsync()) { CompleteSource = source };

        (await verifier.VerifyAsync(request, TestContext.Current.CancellationToken)).IsAuthoritative
            .ShouldBeFalse("a request-carried source is not SDK admission");

        MethodInfo set = typeof(CompleteCommandSourceContext).GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? previous = set.Invoke(null, [source]);
        try
        {
            (await verifier.VerifyAsync(request with { CompleteSource = null }, TestContext.Current.CancellationToken)).IsAuthoritative
                .ShouldBeTrue();
        }
        finally
        {
            set.Invoke(null, [previous]);
        }
    }

    private async Task<CompleteCommandSource> CompleteAsync(F f)
    {
        AuthoritativeEventStream stream = (await f.ReadAsync(Source, TestContext.Current.CancellationToken)).Stream!;
        StreamReadEvent[] events = [.. stream.Events.Select(static e => e with
        {
            NativeEventDigest = Convert.ToHexStringLower(SHA256.HashData(e.Payload)),
        })];
        NativeSourceProof proof = NativeSourceProofCodec.CreateSeed(Source, Incarnation);
        foreach (StreamReadEvent item in events)
        {
            proof = NativeSourceProofCodec.ExtendDigest(proof, Source, item.SequenceNumber, item.MessageId, item.NativeEventDigest!);
        }

        DateTimeOffset now = _clock.GetUtcNow();
        return new CompleteCommandSource(Source, "platform-authority-1", now.AddSeconds(-10), now.AddMinutes(2), proof, events);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
