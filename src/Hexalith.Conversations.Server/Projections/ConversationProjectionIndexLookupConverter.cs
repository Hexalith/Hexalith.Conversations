// <copyright file="ConversationProjectionIndexLookupConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;

using static Hexalith.Conversations.Server.Projections.ConversationProjectionJsonValidation;

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

        if (!UsesStreamingPropertyBinding(options))
        {
            ConversationProjectionIndexReadModel? index = JsonSerializer.Deserialize<ConversationProjectionIndexReadModel>(ref reader, options);
            return index is null ? null : FromFullIndex(index, target.ConversationId);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for the tenant index.");
        }

        ConversationSummaryProjectionV1? matchedSummary = null;
        int matchCount = 0;
        ConversationProjectionDispatchReference? dispatchReference = null;
        bool summariesContainNull = false;
        bool dispatchesAreNull = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                ValidateIndexShape(summariesContainNull, dispatchesAreNull);
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
                summariesContainNull = ReadSummaries(ref reader, target, options, out matchedSummary, out matchCount);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "dispatches"u8))
            {
                dispatchReference = ReadDispatches(ref reader, target, options, out dispatchesAreNull);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "schemaversion"u8))
            {
                ValidateSchemaVersion(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "lifecycle"u8))
            {
                ValidateLifecycle(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "watermarks"u8))
            {
                ValidateWatermarks(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "generationtoken"u8))
            {
                ValidateGenerationToken(ref reader);
            }
            else
            {
                ValidateStringEncoding(ref reader);
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

    /// <summary>Extracts the targeted row and duplicate count from a typed index.</summary>
    internal static ConversationProjectionIndexLookupResult FromFullIndex(
        ConversationProjectionIndexReadModel index,
        string targetId)
    {
        ConversationSummaryProjectionV1? matchedSummary = null;
        int matchCount = 0;
        foreach (ConversationSummaryProjectionV1? summary in index.Summaries ?? [])
        {
            ValidateIndexShape(summary is null, dispatchesAreNull: false);
            if (summary?.ConversationId.Value == targetId)
            {
                matchedSummary ??= summary;
                matchCount++;
            }
        }

        ValidateIndexShape(summariesContainNull: false, index.Dispatches is null);
        _ = index.Dispatches!.TryGetValue(targetId, out ConversationProjectionDispatchReference? dispatchReference);
        return new ConversationProjectionIndexLookupResult
        {
            MatchedSummary = matchedSummary,
            MatchCount = matchCount,
            DispatchReference = dispatchReference,
        };
    }

    private static void ValidateIndexShape(bool summariesContainNull, bool dispatchesAreNull)
    {
        if (summariesContainNull || dispatchesAreNull)
        {
            throw new JsonException("Tenant index summaries must contain objects and dispatches must be an object.");
        }
    }

    private static bool ReadSummaries(
        ref Utf8JsonReader reader,
        ConversationProjectionIndexLookupTarget target,
        JsonSerializerOptions options,
        out ConversationSummaryProjectionV1? matchedSummary,
        out int matchCount)
    {
        // Last-write-wins for duplicate JSON "summaries" properties, matching System.Text.Json behavior.
        matchedSummary = null;
        matchCount = 0;
        bool containsNull = false;

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading summaries.");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return false;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array for tenant index summaries.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return containsNull;
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                containsNull = true;
                continue;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected a JSON object for a tenant index summary element.");
            }

            Utf8JsonReader elementStart = reader;
            bool isMatch = ScanSummaryObjectForTarget(ref reader, target, options);
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
        ConversationProjectionIndexLookupTarget target,
        JsonSerializerOptions options)
    {
        bool matched = false;
        bool hasConversationId = false;
        bool hasTenantId = false;
        bool hasSchemaVersion = false;
        bool hasFreshness = false;
        bool hasLifecycle = false;
        bool validParticipants = true;
        int messageCount = 0;
        int fileReferenceCount = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!hasConversationId || !hasTenantId || !hasSchemaVersion || !hasFreshness || !hasLifecycle
                    || !validParticipants || messageCount < 0 || fileReferenceCount < 0)
                {
                    throw new JsonException("A tenant index summary is missing required fields or has invalid values.");
                }

                return matched;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in a summary element.");
            }

            // Keep the current property token for name matching after advancing to its value.
            Utf8JsonReader name = reader;
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON inside a summary element.");
            }

            if (PropertyNameEqualsIgnoreCase(ref name, "conversationid"u8))
            {
                hasConversationId = IsValidIdentifier<ConversationId>(ref reader, options, "conv:"u8);
                matched = hasConversationId && reader.ValueTextEquals(target.PrefixedIdUtf8);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "tenantid"u8))
            {
                hasTenantId = IsValidIdentifier<TenantId>(ref reader, options, "tenant:"u8);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "schemaversion"u8))
            {
                hasSchemaVersion = JsonSerializer.Deserialize<SchemaVersion>(ref reader, options) is not null;
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "freshness"u8))
            {
                hasFreshness = JsonSerializer.Deserialize<ProjectionFreshnessV1>(ref reader, options) is not null;
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "lifecyclestate"u8))
            {
                ValidateNullableString(ref reader);
                hasLifecycle = reader.TokenType == JsonTokenType.String
                    && (reader.ValueTextEquals("Initializing"u8) || reader.ValueTextEquals("Open"u8)
                        || reader.ValueTextEquals("Closed"u8) || reader.ValueTextEquals("Archived"u8));
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "label"u8))
            {
                ValidateNullableString(ref reader);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "businessreference"u8))
            {
                _ = JsonSerializer.Deserialize<BusinessReference>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "projectid"u8))
            {
                _ = IsValidIdentifier<ProjectId>(ref reader, options, "project:"u8);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "folderid"u8))
            {
                _ = IsValidIdentifier<FolderId>(ref reader, options, "folder:"u8);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "participantpartyids"u8))
            {
                IReadOnlyList<PartyId>? participants = JsonSerializer.Deserialize<IReadOnlyList<PartyId>>(ref reader, options);
                validParticipants = participants is null || !participants.Any(party => party is null);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "messagecount"u8))
            {
                messageCount = ReadInt32(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "filereferencecount"u8))
            {
                fileReferenceCount = ReadInt32(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "providercorrelation"u8))
            {
                _ = JsonSerializer.Deserialize<ProviderCorrelationMetadata>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref name, "searchtrustpreview"u8))
            {
                _ = JsonSerializer.Deserialize<ConversationSearchTrustPreviewV1>(ref reader, options);
            }
            else
            {
                ValidateStringEncoding(ref name);
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning a summary element.");
    }

    private static bool IsValidIdentifier<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        ReadOnlySpan<byte> prefix)
        where T : class
    {
        // These contracts accept any non-whitespace identifier suffix. Let the contract converter
        // validate escaped, non-ASCII and invalid tokens.
        if (reader.TokenType == JsonTokenType.String && !reader.ValueIsEscaped && !reader.HasValueSequence)
        {
            ReadOnlySpan<byte> text = reader.ValueSpan;
            if (text.StartsWith(prefix) && text.Length > prefix.Length && System.Text.Ascii.IsValid(text)
                && text[prefix.Length..].IndexOfAnyExcept((byte)' ') >= 0)
            {
                return true;
            }
        }

        return JsonSerializer.Deserialize<T>(ref reader, options) is not null;
    }

    private static ConversationProjectionDispatchReference? ReadDispatches(
        ref Utf8JsonReader reader,
        ConversationProjectionIndexLookupTarget target,
        JsonSerializerOptions options,
        out bool isNull)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading dispatches.");
        }

        isNull = reader.TokenType == JsonTokenType.Null;
        if (isNull)
        {
            return null;
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

            ValidateStringEncoding(ref reader);
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
                ValidateSiblingDispatchReference(ref reader, options);
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning dispatches.");
    }

    private static void ValidateSchemaVersion(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index schemaVersion.");
        }

        _ = ReadInt32(ref reader, options);
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

    private static void ValidateWatermarks(ref Utf8JsonReader reader, JsonSerializerOptions options)
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

            ValidateStringEncoding(ref reader);
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading a tenant index watermark value.");
            }

            _ = ReadInt64(ref reader, options);
        }

        throw new JsonException("Unexpected end of JSON while scanning tenant index watermarks.");
    }

    private static void ValidateGenerationToken(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading tenant index generationToken.");
        }

        ValidateNullableString(ref reader);
    }

    private static void ValidateSiblingDispatchReference(ref Utf8JsonReader reader, JsonSerializerOptions options)
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

            ValidateStringEncoding(ref reader);
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
                ValidateNullableString(ref reader);
            }
            else if (isLastPosition)
            {
                _ = ReadInt64(ref reader, options);
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
                if (reader.TokenType != JsonTokenType.Null)
                {
                    _ = ReadInt64(ref reader, options);
                }
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while scanning a tenant index dispatch reference.");
    }

    private static int ReadInt32(ref Utf8JsonReader reader, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value)
            ? value
            : JsonSerializer.Deserialize<int>(ref reader, options);

    private static long ReadInt64(ref Utf8JsonReader reader, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long value)
            ? value
            : JsonSerializer.Deserialize<long>(ref reader, options);
}
