// <copyright file="ConversationIndexGenerationToken.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Computes the canonical, order-independent SHA-256 generation token over a tenant's conversation summaries
/// using a bounded streaming <see cref="IncrementalHash"/> so reads and writes never allocate full serialized
/// index byte arrays while preserving byte-for-byte digest compatibility with <see cref="JsonSerializer.SerializeToUtf8Bytes{TValue}(TValue, JsonSerializerOptions?)"/>.
/// </summary>
internal static class ConversationIndexGenerationToken
{
    /// <summary>The token returned for an empty summary set.</summary>
    internal const string EmptyToken = "empty";

    private const int FlushThresholdBytes = 8192;

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        SkipValidation = true,
    };

    private static readonly JsonTypeInfo<ConversationSummaryProjectionV1> SummaryTypeInfo =
        (JsonTypeInfo<ConversationSummaryProjectionV1>)JsonSerializerOptions.Default.GetTypeInfo(
            typeof(ConversationSummaryProjectionV1));

    /// <summary>
    /// Validates that a persisted generation token is a 64-character lowercase hexadecimal SHA-256 digest.
    /// </summary>
    /// <param name="token">The candidate token.</param>
    /// <returns><see langword="true"/> when the token is a valid 64-char lowercase hex string; otherwise <see langword="false"/>.</returns>
    internal static bool IsValidPersistedToken(string? token)
    {
        if (token is not { Length: 64 })
        {
            return false;
        }

        for (int index = 0; index < token.Length; index++)
        {
            char c = token[index];
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the supplied summaries are already ordered ordinally by <see cref="Contracts.Identifiers.ConversationId.Value"/>.
    /// </summary>
    /// <param name="summaries">The summaries to inspect.</param>
    /// <returns><see langword="true"/> when already non-decreasing in ordinal order.</returns>
    internal static bool IsOrdered(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        for (int index = 1; index < summaries.Count; index++)
        {
            if (StringComparer.Ordinal.Compare(
                summaries[index - 1].ConversationId.Value,
                summaries[index].ConversationId.Value) > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes the deterministic lowercase-hex SHA-256 generation token over all fields of the supplied summaries
    /// ordered ordinally by <see cref="Contracts.Identifiers.ConversationId.Value"/>, streaming the exact default
    /// <see cref="JsonSerializer"/> representation through a bounded pooled buffer.
    /// </summary>
    /// <param name="summaries">The tenant-scoped conversation summaries.</param>
    /// <returns>The canonical generation token, or <see cref="EmptyToken"/> when empty.</returns>
    internal static string Compute(IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        int count = summaries.Count;
        if (count == 0)
        {
            return EmptyToken;
        }

        IReadOnlyList<ConversationSummaryProjectionV1> ordered = EnsureOrdered(summaries);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using IncrementalHashBufferWriter buffer = new(hash);
        using Utf8JsonWriter writer = new(buffer, WriterOptions);

        writer.WriteStartArray();
        for (int index = 0; index < ordered.Count; index++)
        {
            JsonSerializer.Serialize(writer, ordered[index], SummaryTypeInfo);
            if (writer.BytesPending >= FlushThresholdBytes)
            {
                writer.Flush();
            }
        }

        writer.WriteEndArray();
        writer.Flush();
        buffer.Flush();

        Span<byte> digest = stackalloc byte[32];
        _ = hash.TryGetHashAndReset(digest, out _);
        return Convert.ToHexStringLower(digest);
    }

    private static IReadOnlyList<ConversationSummaryProjectionV1> EnsureOrdered(
        IReadOnlyList<ConversationSummaryProjectionV1> summaries)
    {
        if (IsOrdered(summaries))
        {
            return summaries;
        }

        ConversationSummaryProjectionV1[] sorted = new ConversationSummaryProjectionV1[summaries.Count];
        for (int index = 0; index < summaries.Count; index++)
        {
            sorted[index] = summaries[index];
        }

        Array.Sort(
            sorted,
            static (left, right) => StringComparer.Ordinal.Compare(left.ConversationId.Value, right.ConversationId.Value));
        return sorted;
    }
}
