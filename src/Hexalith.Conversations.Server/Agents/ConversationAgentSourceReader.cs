// <copyright file="ConversationAgentSourceReader.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Complete authoritative source replay, never projection authority.</summary>
/// <param name="streams">Authenticated bounded stable-head stream reader.</param>
public sealed class ConversationAgentSourceReader(IAuthoritativeEventStreamReader streams)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, Type> EventTypes = typeof(ConversationState)
        .GetMethods().Where(m => m.Name == "Apply" && m.GetParameters().Length == 1)
        .Select(m => m.GetParameters()[0].ParameterType)
        .ToDictionary(t => t.FullName!, StringComparer.Ordinal);

    /// <summary>Reads an exact complete source; malformed or uncertified prefixes are unavailable.</summary>
    /// <param name="tenant">Authenticated tenant.</param>
    /// <param name="conversation">Exact authorized source.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The complete source and replayed state, or null.</returns>
    public async Task<(AuthoritativeEventStream Source, ConversationState State)?> ReadAsync(TenantId tenant,
        ConversationId conversation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var identity = new AggregateIdentity(tenant.Value, "conversation", conversation.Value);
        try
        {
            AuthoritativeStreamReadResult result = await streams.ReadAsync(identity, cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result is { IsAuthoritative: true } ? ReplaySource(result.Stream, identity, cancellationToken) : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Validates and replays an already authenticated complete SDK prefix without transport calls.</summary>
    /// <param name="sourceInput">Certified complete source.</param>
    /// <param name="identity">Exact expected source.</param>
    /// <returns>The valid complete state or null.</returns>
    public static (AuthoritativeEventStream Source, ConversationState State)? ReplaySource(AuthoritativeEventStream? sourceInput,
        AggregateIdentity identity)
        => ReplaySource(sourceInput, identity, CancellationToken.None);

    /// <summary>Replays the same exact source with caller-first completion and iteration cancellation.</summary>
    /// <param name="sourceInput">Certified complete source.</param>
    /// <param name="identity">Exact expected source.</param>
    /// <param name="cancellationToken">Cancellation observed during capture and before state release.</param>
    /// <returns>The valid complete state or null.</returns>
    public static (AuthoritativeEventStream Source, ConversationState State)? ReplaySource(AuthoritativeEventStream? sourceInput,
        AggregateIdentity identity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = ReplaySourceCore(sourceInput, identity, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static (AuthoritativeEventStream Source, ConversationState State)? ReplaySourceCore(AuthoritativeEventStream? sourceInput,
        AggregateIdentity identity, CancellationToken cancellationToken)
    {
        var tenant = new TenantId(identity.TenantId);
        var conversation = new ConversationId(identity.AggregateId);
        if (sourceInput is not { } source || source.Identity != identity || identity.Domain != "conversation"
            || source.Events is null || source.Head != source.Events.Count
            || source.Head < 0 || source.ObservedAt == default || string.IsNullOrWhiteSpace(source.ObservationId))
        {
            return null;
        }
        if (source.Head == 0)
        {
            return (source, new ConversationState());
        }
        try
        {
            var records = new List<ConversationReplayEventRecord>();
            foreach (StreamReadEvent item in source.Events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item is null || item.Payload is null
                    || item.ProtectionMetadata is { } metadata && (metadata.State is not (PayloadProtectionState.Unprotected or PayloadProtectionState.Protected)
                        || !EventStorePayloadProtectionMetadataCarrier.TryValidate(metadata, out _)) || string.IsNullOrWhiteSpace(item.MessageId) || item.SequenceNumber != records.Count + 1 || item.MetadataVersion != 1
                    || !string.Equals(item.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase)
                    || !EventTypes.TryGetValue(item.EventTypeName, out Type? type))
                {
                    return null;
                }
                object? value = JsonSerializer.Deserialize(item.Payload, type, JsonOptions);
                if (value is null)
                {
                    return null;
                }
                cancellationToken.ThrowIfCancellationRequested();
                records.Add(new(item.SequenceNumber, value));
            }
            cancellationToken.ThrowIfCancellationRequested();
            ConversationReplayResult replay = ConversationReplayVerifier.Replay(tenant, conversation, CancellationChecked(records, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return replay.State is { HasCompleteEventPrefix: true } state && state.SourceRevision == source.Head
                ? (source, state) : null;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or TargetInvocationException)
        {
            return null;
        }
    }

    private static IEnumerable<ConversationReplayEventRecord> CancellationChecked(IEnumerable<ConversationReplayEventRecord> records,
        CancellationToken cancellationToken)
    {
        using var iterator = records.GetEnumerator();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!iterator.MoveNext()) { yield break; }
            cancellationToken.ThrowIfCancellationRequested();
            yield return iterator.Current;
        }
    }

}
