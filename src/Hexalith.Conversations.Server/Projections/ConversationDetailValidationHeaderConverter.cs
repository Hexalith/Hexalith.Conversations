// <copyright file="ConversationDetailValidationHeaderConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Streaming <see cref="Utf8JsonReader"/> converter for <see cref="ConversationDetailValidationHeader"/> that
/// validates all required and recognized <see cref="ConversationDetailProjectionV1"/> fields fail-closed
/// without allocating full timeline, participant, file-reference, or evidence CLR object graphs.
/// </summary>
internal sealed class ConversationDetailValidationHeaderConverter : JsonConverter<ConversationDetailValidationHeader>
{
    /// <inheritdoc/>
    public override ConversationDetailValidationHeader? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for conversation detail.");
        }

        SchemaVersion? schemaVersion = null;
        TenantId? tenantId = null;
        ConversationId? conversationId = null;
        ProjectionFreshnessV1? freshness = null;
        string? lifecycleState = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (schemaVersion is null
                    || tenantId is null
                    || conversationId is null
                    || freshness is null
                    || lifecycleState is null)
                {
                    throw new JsonException("Conversation detail is missing one or more required header fields.");
                }

                return new ConversationDetailValidationHeader(
                    schemaVersion,
                    tenantId,
                    conversationId,
                    freshness,
                    lifecycleState);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in conversation detail.");
            }

            if (PropertyNameEqualsIgnoreCase(ref reader, "schemaversion"u8))
            {
                schemaVersion = DeserializeRequired<SchemaVersion>(ref reader, options, "schemaVersion");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "tenantid"u8))
            {
                tenantId = DeserializeRequired<TenantId>(ref reader, options, "tenantId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "conversationid"u8))
            {
                conversationId = DeserializeRequired<ConversationId>(ref reader, options, "conversationId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "freshness"u8))
            {
                freshness = DeserializeRequired<ProjectionFreshnessV1>(ref reader, options, "freshness");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "lifecyclestate"u8))
            {
                ReadValueToken(ref reader, "lifecycleState");
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("Expected a JSON string for detail lifecycleState.");
                }

                lifecycleState = ValidateLifecycle(reader.GetString());
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "label"u8))
            {
                ReadValueToken(ref reader, "label");
                if (reader.TokenType is not (JsonTokenType.Null or JsonTokenType.String))
                {
                    throw new JsonException("Expected a JSON string or null for detail label.");
                }
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "businessreference"u8))
            {
                ValidateOptionalObject<BusinessReference>(ref reader, options, "businessReference");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "projectid"u8))
            {
                ValidateOptionalObject<ProjectId>(ref reader, options, "projectId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "folderid"u8))
            {
                ValidateOptionalObject<FolderId>(ref reader, options, "folderId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "providercorrelation"u8))
            {
                ValidateOptionalObject<ProviderCorrelationMetadata>(ref reader, options, "providerCorrelation");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "activeretentionpolicy"u8))
            {
                ValidateOptionalObject<ConversationRetentionPolicyProjectionV1>(ref reader, options, "activeRetentionPolicy");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "trustposture"u8))
            {
                ValidateOptionalObject<ConversationEvidenceTrustPostureV1>(ref reader, options, "trustPosture");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "attributes"u8))
            {
                ValidateAttributes(ref reader);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "participants"u8)
                || PropertyNameEqualsIgnoreCase(ref reader, "messages"u8)
                || PropertyNameEqualsIgnoreCase(ref reader, "filereferences"u8)
                || PropertyNameEqualsIgnoreCase(ref reader, "sensitivitymarks"u8)
                || PropertyNameEqualsIgnoreCase(ref reader, "redactions"u8)
                || PropertyNameEqualsIgnoreCase(ref reader, "evidenceentries"u8))
            {
                ValidateNonNullObjectArray(ref reader);
            }
            else
            {
                ReadValueToken(ref reader, "unknown");
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of JSON while reading conversation detail.");
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        ConversationDetailValidationHeader value,
        JsonSerializerOptions options)
        => throw new NotSupportedException("Conversation detail validation headers are read-only.");

    private static T DeserializeRequired<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        string fieldName)
        where T : class
    {
        ReadValueToken(ref reader, fieldName);
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException($"Required detail field '{fieldName}' must not be null.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(ref reader, options)
                ?? throw new JsonException($"Required detail field '{fieldName}' must not be null.");
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Detail field '{fieldName}' failed validation.", ex);
        }
    }

    private static void ValidateOptionalObject<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        string fieldName)
        where T : class
    {
        ReadValueToken(ref reader, fieldName);
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        try
        {
            _ = JsonSerializer.Deserialize<T>(ref reader, options);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Detail field '{fieldName}' failed validation.", ex);
        }
    }

    private static string ValidateLifecycle(string? value)
    {
        if (value is not ("Initializing" or "Open" or "Closed" or "Archived"))
        {
            throw new JsonException("Unsupported or missing conversation detail lifecycleState.");
        }

        return value;
    }

    private static void ValidateAttributes(ref Utf8JsonReader reader)
    {
        ReadValueToken(ref reader, "attributes");
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object or null for detail attributes.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in detail attributes.");
            }

            if (IsWhitespacePropertyName(ref reader))
            {
                throw new JsonException("Detail attribute keys must be non-empty.");
            }

            ReadValueToken(ref reader, "attributes value");
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Detail attribute values must be non-null JSON strings.");
            }
        }

        throw new JsonException("Unexpected end of JSON while reading detail attributes.");
    }

    private static void ValidateNonNullObjectArray(ref Utf8JsonReader reader)
    {
        ReadValueToken(ref reader, "collection");
        if (reader.TokenType == JsonTokenType.Null)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array or null for detail collection.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Detail collection elements must be non-null JSON objects.");
            }

            reader.Skip();
        }

        throw new JsonException("Unexpected end of JSON while reading detail collection.");
    }

    private static void ReadValueToken(ref Utf8JsonReader reader, string fieldName)
    {
        if (!reader.Read())
        {
            throw new JsonException($"Unexpected end of JSON while reading detail field '{fieldName}'.");
        }
    }

    private static bool IsWhitespacePropertyName(ref Utf8JsonReader reader)
    {
        if (!reader.HasValueSequence)
        {
            ReadOnlySpan<byte> span = reader.ValueSpan;
            if (span.IndexOf((byte)'\\') < 0)
            {
                for (int index = 0; index < span.Length; index++)
                {
                    byte b = span[index];
                    if (b >= 0x80)
                    {
                        return string.IsNullOrWhiteSpace(reader.GetString());
                    }

                    if (b > (byte)' ')
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return string.IsNullOrWhiteSpace(reader.GetString());
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
