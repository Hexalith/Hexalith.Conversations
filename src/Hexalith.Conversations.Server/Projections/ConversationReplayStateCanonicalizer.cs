// <copyright file="ConversationReplayStateCanonicalizer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Produces the canonical bytes and hash of deterministic replay-state values through EventStore's fixed
/// coordinated-batch canonicalizer (AD-8).
/// </summary>
/// <remarks>
/// <para>
/// The canonicalizer sorts object properties ordinally but keeps array order, so array order must already be an
/// explicit, stable choice. Values are first projected with the same fixed web defaults plus one guard: a type
/// that is an unordered set (<see cref="ISet{T}"/> or <see cref="IReadOnlySet{T}"/>) has no stable composite key,
/// so it fails here, before persistence, instead of producing bytes that depend on hash iteration order.
/// Dictionaries are accepted because they become JSON objects whose keys the canonicalizer orders.
/// </para>
/// <para>
/// Conversations source-generated API options are deliberately not used: the batch seam's options are fixed.
/// </para>
/// </remarks>
internal static class ConversationReplayStateCanonicalizer
{
    private static readonly JsonSerializerOptions OrderedWebOptions = CreateOrderedWebOptions();

    /// <summary>Projects a value to a JSON tree, failing on any unordered collection type.</summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The JSON tree.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value contains an unordered collection.</exception>
    internal static JsonNode ToOrderedNode<TValue>(TValue value)
        => JsonSerializer.SerializeToNode(value, OrderedWebOptions)
            ?? throw new InvalidOperationException("A replay-state value must not serialize to null.");

    /// <summary>Returns EventStore's canonical UTF-8 bytes for a replay-state tree.</summary>
    /// <param name="state">The replay-state tree.</param>
    /// <returns>The canonical bytes.</returns>
    internal static byte[] Canonicalize(JsonNode state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ReadModelBatchCanonicalJson.Serialize(state).ToArray();
    }

    /// <summary>Computes the lowercase-hex SHA-256 of exact canonical bytes.</summary>
    /// <param name="canonicalBytes">The canonical bytes.</param>
    /// <returns>The 64-character lowercase hexadecimal digest.</returns>
    internal static string Hash(ReadOnlySpan<byte> canonicalBytes)
        => EventStorePayloadFingerprint.Compute(canonicalBytes);

    private static JsonSerializerOptions CreateOrderedWebOptions()
    {
        DefaultJsonTypeInfoResolver resolver = new();
        resolver.Modifiers.Add(static typeInfo =>
        {
            if (IsUnorderedCollection(typeInfo.Type))
            {
                throw new InvalidOperationException(
                    "A replay-state value contains an unordered collection without a stable composite key.");
            }
        });
        return new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver };
    }

    private static bool IsUnorderedCollection(Type type)
        => type.GetInterfaces()
            .Append(type)
            .Any(static candidate => candidate.IsGenericType
                && (candidate.GetGenericTypeDefinition() == typeof(ISet<>)
                    || candidate.GetGenericTypeDefinition() == typeof(IReadOnlySet<>)));
}
