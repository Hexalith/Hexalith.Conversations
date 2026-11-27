// <copyright file="ConversationProjectionIndexLookupConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Streaming <see cref="Utf8JsonReader"/> converter that extracts only the targeted conversation's summary
/// (while preserving duplicate-entry detection) and dispatch reference from a persisted tenant index.
/// </summary>
internal sealed class ConversationProjectionIndexLookupConverter : JsonConverter<ConversationProjectionIndexLookupResult>
{
    /// <inheritdoc/>
    public override ConversationProjectionIndexLookupResult? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ConversationProjectionIndexLookupTarget target = ConversationProjectionIndexLookupScope.Current
            ?? throw new InvalidOperationException("A lookup target scope is required to read a targeted tenant index.");

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for the tenant index.");
        }

        ConversationSummaryProjectionV1? matchedSummary = null;
        int matchCount = 0;
        ConversationProjectionDispatchReference? dispatchReference = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new ConversationProjectionIndexLookupResult
                {
                    MatchedSummary = matchedSummary,
                    MatchCount = matchCount,
                    DispatchReference = dispatchReference,
                };
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in the tenant index.");
            }

            if (PropertyNameEqualsIgnoreCase(ref reader, "summaries"u8))
            {
                ReadSummaries(ref reader, target, options, ref matchedSummary, ref matchCount);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "dispatches"u8))
            {
                dispatchReference = ReadDispatches(ref reader, target, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "schemaversion"u8))
            {
                ValidateSchemaVersion(ref reader);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "lifecycle"u8))
            {
                ValidateLifecycle(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "watermarks"u8))
            {
                ValidateWatermarks(ref reader);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "generationtoken"u8))
            {
                ValidateGenerationToken(ref reader);
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON in the tenant index.");
                }

                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while reading the tenant index.");
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        ConversationProjectionIndexLookupResult value,
        JsonSerializerOptions options)
        => throw new NotSupportedException("Targeted tenant index lookup results are read-only.");

    private static void ReadSummaries(
        ref Utf8JsonReader reader,
        ConversationProjectionIndexLookupTarget target,
        JsonSerializerOptions options,
        ref ConversationSummaryProjectionV1? matchedSummary,
        ref int matchCount)
    {
        // Last-write-wins for duplicate JSON "summaries" properties, matching System.Text.Json behavior.
        matchedSummary = null;
        matchCount = 0;

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading summaries.");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array for tenant index summaries.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected a JSON object for a tenant index summary element.");
            }

            Utf8JsonReader elementStart = reader;
            bool isMatch = ScanSummaryObjectForTarget(ref reader, target);
            if (isMatch)
            {
                matchCount++;
                if (matchCount == 1)
                {
                    ConversationSummaryProjectionV1? deserialized =
                        JsonSerializer.Deserialize<ConversationSummaryProjectionV1>(ref elementStart, options);
                    if (deserialized is null
                        || !string.Equals(deserialized.ConversationId.Value, target.ConversationId, StringComparison.Ordinal))
                    {
                        throw new JsonException("Deserialized summary conversationId did not match lookup target.");
                    }

                    matchedSummary = deserialized;
                }
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning summaries.");
    }

    private static bool ScanSummaryObjectForTarget(
        ref Utf8JsonReader reader,
        ConversationProjectionIndexLookupTarget target)
    {
        bool matched = false;
        bool hasConversationId = false;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!hasConversationId)
                {
                    throw new JsonException("Expected conversationId in every tenant index summary element.");
                }

                return matched;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in a summary element.");
            }

            bool isConversationId = PropertyNameEqualsIgnoreCase(ref reader, "conversationid"u8);
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON inside a summary element.");
            }

            if (isConversationId)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("Expected a JSON string for summary conversationId.");
                }

                hasConversationId = true;
                // Last-write-wins if a JSON object repeats conversationId.
                matched = reader.ValueTextEquals(target.PrefixedIdUtf8);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning a summary element.");
    }

    private static ConversationProjectionDispatchReference? ReadDispatches(
        ref Utf8JsonReader reader,
        ConversationProjectionIndexLookupTarget target,
        JsonSerializerOptions options)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading dispatches.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for tenant index dispatches.");
        }

        ConversationProjectionDispatchReference? found = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return found;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in tenant index dispatches.");
            }

            bool isTarget = reader.ValueTextEquals(target.RawIdUtf8);
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a dispatch entry.");
            }

            if (isTarget)
            {
                found = JsonSerializer.Deserialize<ConversationProjectionDispatchReference>(ref reader, options);
            }
            else
            {
                ValidateSiblingDispatchReference(ref reader);
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning dispatches.");
    }

    private static void ValidateSchemaVersion(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index schemaVersion.");
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out _))
        {
            throw new JsonException("Expected a 32-bit integer for tenant index schemaVersion.");
        }
    }

    private static void ValidateLifecycle(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index lifecycle.");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        _ = JsonSerializer.Deserialize<ConversationProjectionIndexLifecycle?>(ref reader, options);
    }

    private static void ValidateWatermarks(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index watermarks.");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object or null for tenant index watermarks.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in tenant index watermarks.");
            }

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a tenant index watermark value.");
            }

            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out _))
            {
                throw new JsonException("Expected a 64-bit integer value in tenant index watermarks.");
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning tenant index watermarks.");
    }

    private static void ValidateGenerationToken(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index generationToken.");
        }

        if (reader.TokenType is not (JsonTokenType.Null or JsonTokenType.String))
        {
            throw new JsonException("Expected a JSON string or null for tenant index generationToken.");
        }
    }

    private static void ValidateSiblingDispatchReference(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object or null for a tenant index dispatch reference.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in a tenant index dispatch reference.");
            }

            bool isDispatchId = PropertyNameEqualsIgnoreCase(ref reader, "dispatchid"u8);
            bool isLastPosition = !isDispatchId && PropertyNameEqualsIgnoreCase(ref reader, "lastappliedeventposition"u8);
            bool isPending = !isDispatchId && !isLastPosition && PropertyNameEqualsIgnoreCase(ref reader, "ispending"u8);
            bool isPrevDispatch = !isDispatchId && !isLastPosition && !isPending && PropertyNameEqualsIgnoreCase(ref reader, "previousdispatchid"u8);
            bool isPrevPosition = !isDispatchId && !isLastPosition && !isPending && !isPrevDispatch
                && PropertyNameEqualsIgnoreCase(ref reader, "previouslastappliedeventposition"u8);

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON inside a tenant index dispatch reference.");
            }

            if (isDispatchId || isPrevDispatch)
            {
                if (reader.TokenType is not (JsonTokenType.Null or JsonTokenType.String))
                {
                    throw new JsonException("Expected a JSON string or null for dispatch reference identifier.");
                }
            }
            else if (isLastPosition)
            {
                if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out _))
                {
                    throw new JsonException("Expected a 64-bit integer for dispatch reference lastAppliedEventPosition.");
                }
            }
            else if (isPending)
            {
                if (reader.TokenType is not (JsonTokenType.True or JsonTokenType.False))
                {
                    throw new JsonException("Expected a JSON boolean for dispatch reference isPending.");
                }
            }
            else if (isPrevPosition)
            {
                if (reader.TokenType != JsonTokenType.Null
                    && (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out _)))
                {
                    throw new JsonException("Expected a 64-bit integer or null for dispatch reference previousLastAppliedEventPosition.");
                }
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning a tenant index dispatch reference.");
    }

    private static bool PropertyNameEqualsIgnoreCase(ref Utf8JsonReader reader, ReadOnlySpan<byte> lowerAsciiName)
    {
        if (!reader.HasValueSequence)
        {
            ReadOnlySpan<byte> span = reader.ValueSpan;
            if (span.Length == lowerAsciiName.Length && span.IndexOf((byte)'\\') < 0)
            {
                for (int index = 0; index < span.Length; index++)
                {
                    byte b = span[index];
                    if ((uint)(b - 'A') <= ('Z' - 'A'))
                    {
                        b = (byte)(b | 0x20);
                    }

                    if (b != lowerAsciiName[index])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        string? text = reader.GetString();
        if (text is null || text.Length != lowerAsciiName.Length)
        {
            return false;
        }

        for (int index = 0; index < lowerAsciiName.Length; index++)
        {
            char c = text[index];
            if ((uint)(c - 'A') <= ('Z' - 'A'))
            {
                c = (char)(c | 0x20);
            }

            if (c != lowerAsciiName[index])
            {
                return false;
            }
        }

        return true;
    }
}
