// <copyright file="CompleteSourceConversationCommandSourceVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Maps the SDK-admitted complete command source of the current processing invocation to an authoritative stream.</summary>
/// <remarks>Only <see cref="CompleteCommandSourceContext.Current"/> is read: the SDK installs it after authenticating the
/// platform-bound native prefix. The request's own <c>CompleteSource</c>, <c>CommandStateProof</c> and
/// <c>VerifiedEffectiveEvents</c> are never authority. No storage read or actor re-entry happens here.</remarks>
public sealed class CompleteSourceConversationCommandSourceVerifier : IConversationCommandSourceVerifier
{
    private readonly TimeProvider _clock;
    private readonly Func<CompleteCommandSource?> _current;

    /// <summary>Initializes a new instance of the <see cref="CompleteSourceConversationCommandSourceVerifier"/> class.</summary>
    /// <param name="clock">The expiry clock.</param>
    public CompleteSourceConversationCommandSourceVerifier(TimeProvider clock)
        : this(clock, static () => CompleteCommandSourceContext.Current)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CompleteSourceConversationCommandSourceVerifier"/> class with an explicit admitted-source accessor.</summary>
    /// <param name="clock">The expiry clock.</param>
    /// <param name="current">Returns the SDK-admitted source of the current invocation.</param>
    internal CompleteSourceConversationCommandSourceVerifier(TimeProvider clock, Func<CompleteCommandSource?> current)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(current);
        _clock = clock;
        _current = current;
    }

    /// <inheritdoc />
    public Task<AuthoritativeStreamReadResult> VerifyAsync(DomainServiceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        AuthoritativeStreamReadResult result = Map(request, _current(), _clock.GetUtcNow());
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(result);
    }

    private static AuthoritativeStreamReadResult Map(DomainServiceRequest request, CompleteCommandSource? source, DateTimeOffset now)
    {
        if (request.CommandStateProof is not null || request.VerifiedEffectiveEvents is not null)
        {
            return Unavailable("complete-source-caller-proof-refused");
        }

        if (source is null)
        {
            return Unavailable("complete-source-absent");
        }

        AggregateIdentity identity;
        try
        {
            identity = request.Command?.AggregateIdentity ?? throw new ArgumentException("A command is required.", nameof(request));
        }
        catch (ArgumentException)
        {
            return Unavailable("complete-source-request-invalid");
        }

        if (identity.Domain != "conversation" || source.Identity != identity)
        {
            return Unavailable("complete-source-foreign");
        }

        if (source.ObservedAt == default || source.ObservedAt > now || source.ValidUntil <= now || source.ValidUntil <= source.ObservedAt)
        {
            return Unavailable("complete-source-expired");
        }

        NativeSourceProof? proof = source.Proof;
        if (proof is null || proof.Erased)
        {
            return Unavailable("complete-source-erased");
        }

        IReadOnlyList<StreamReadEvent>? supplied = source.Events;
        if (supplied is null || supplied.Count > CompleteCommandSource.MaximumEvents)
        {
            return Unavailable("complete-source-gap");
        }

        var events = new List<StreamReadEvent>(supplied.Count);
        foreach (StreamReadEvent item in supplied)
        {
            if (item is null || item.Payload is null || item.SequenceNumber != events.Count + 1L)
            {
                return Unavailable("complete-source-gap");
            }

            events.Add(item with { Payload = item.Payload.ToArray() });
        }

        if (proof.HeadSequence != events.Count || !NativeSourceProofCodec.IsValid(proof, identity, events.Count, 1)
            || (events.Count == 0 ? proof.HeadMessageId is not null : proof.HeadMessageId != events[^1].MessageId))
        {
            return Unavailable("complete-source-proof-mismatch");
        }

        try
        {
            NativeSourceProof chain = NativeSourceProofCodec.CreateSeed(identity, proof.SourceIncarnation);
            foreach (StreamReadEvent item in events)
            {
                if (item.NativeEventDigest is null)
                {
                    return Unavailable("complete-source-proof-mismatch");
                }

                chain = NativeSourceProofCodec.ExtendDigest(chain, identity, item.SequenceNumber, item.MessageId, item.NativeEventDigest);
            }

            if (chain != proof)
            {
                return Unavailable("complete-source-proof-mismatch");
            }
        }
        catch (ArgumentException)
        {
            return Unavailable("complete-source-proof-mismatch");
        }

        var stream = new AuthoritativeEventStream(identity, proof.HeadSequence, source.ObservedAt, events.AsReadOnly(),
            "complete-command-source:" + proof.SourceIncarnation + ":" + proof.CumulativeDigest)
        {
            NativeProof = proof,
        };
        return new AuthoritativeStreamReadResult(stream, null);
    }

    private static AuthoritativeStreamReadResult Unavailable(string reason) => new(null, reason);
}
