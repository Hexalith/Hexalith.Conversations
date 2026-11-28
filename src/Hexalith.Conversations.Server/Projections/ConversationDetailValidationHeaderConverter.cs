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

using static Hexalith.Conversations.Server.Projections.ConversationProjectionJsonValidation;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Streaming <see cref="Utf8JsonReader"/> converter for <see cref="ConversationDetailValidationHeader"/> that
/// validates all required and recognized <see cref="ConversationDetailProjectionV1"/> fields fail-closed
/// without assembling timeline, participant, file-reference, or evidence collections.
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

        if (!UsesStreamingPropertyBinding(options))
        {
            ConversationDetailProjectionV1? detail = JsonSerializer.Deserialize<ConversationDetailProjectionV1>(ref reader, options);
            return detail is null
                ? null
                : new ConversationDetailValidationHeader(
                    detail.SchemaVersion, detail.TenantId, detail.ConversationId, detail.Freshness, detail.LifecycleState);
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
        IReadOnlyDictionary<string, string>? attributes = null;
        bool participantsValid = true;
        bool messagesValid = true;
        bool fileReferencesValid = true;
        bool sensitivityMarksValid = true;
        bool redactionsValid = true;
        bool evidenceEntriesValid = true;

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

                if (!participantsValid || !messagesValid || !fileReferencesValid
                    || !sensitivityMarksValid || !redactionsValid || !evidenceEntriesValid)
                {
                    throw new JsonException("Detail collection elements must be non-null.");
                }

                if (attributes is not null
                    && attributes.Any(attribute => string.IsNullOrWhiteSpace(attribute.Key) || attribute.Value is null))
                {
                    throw new JsonException("Detail attribute keys must be non-empty and values must be non-null.");
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
                schemaVersion = ReadValue<SchemaVersion>(ref reader, options, "schemaVersion");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "tenantid"u8))
            {
                tenantId = ReadValue<TenantId>(ref reader, options, "tenantId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "conversationid"u8))
            {
                conversationId = ReadValue<ConversationId>(ref reader, options, "conversationId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "freshness"u8))
            {
                freshness = ReadValue<ProjectionFreshnessV1>(ref reader, options, "freshness");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "lifecyclestate"u8))
            {
                lifecycleState = ReadValue<string>(ref reader, options, "lifecycleState");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "label"u8))
            {
                ReadValueToken(ref reader, "label");
                ValidateNullableString(ref reader);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "businessreference"u8))
            {
                _ = ReadValue<BusinessReference>(ref reader, options, "businessReference");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "projectid"u8))
            {
                _ = ReadValue<ProjectId>(ref reader, options, "projectId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "folderid"u8))
            {
                _ = ReadValue<FolderId>(ref reader, options, "folderId");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "providercorrelation"u8))
            {
                _ = ReadValue<ProviderCorrelationMetadata>(ref reader, options, "providerCorrelation");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "activeretentionpolicy"u8))
            {
                _ = ReadValue<ConversationRetentionPolicyProjectionV1>(ref reader, options, "activeRetentionPolicy");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "trustposture"u8))
            {
                _ = ReadValue<ConversationEvidenceTrustPostureV1>(ref reader, options, "trustPosture");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "attributes"u8))
            {
                attributes = ReadValue<IReadOnlyDictionary<string, string>>(ref reader, options, "attributes");
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "participants"u8))
            {
                participantsValid = ValidateCollection<ConversationParticipantProjectionV1>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "messages"u8))
            {
                messagesValid = ValidateCollection<ConversationTimelineMessageProjectionV1>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "filereferences"u8))
            {
                fileReferencesValid = ValidateCollection<ConversationFileReferenceProjectionV1>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "sensitivitymarks"u8))
            {
                sensitivityMarksValid = ValidateCollection<ConversationSensitivityMarkProjectionV1>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "redactions"u8))
            {
                redactionsValid = ValidateCollection<ConversationRedactionProjectionV1>(ref reader, options);
            }
            else if (PropertyNameEqualsIgnoreCase(ref reader, "evidenceentries"u8))
            {
                evidenceEntriesValid = ValidateCollection<ConversationEvidenceEntryV1>(ref reader, options);
            }
            else
            {
                ValidateStringEncoding(ref reader);
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

    private static T? ReadValue<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        string fieldName)
        where T : class
    {
        ReadValueToken(ref reader, fieldName);
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    private static bool ValidateCollection<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
        where T : class
    {
        ReadValueToken(ref reader, "collection");
        if (reader.TokenType == JsonTokenType.Null)
        {
            return true;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected a JSON array or null for detail collection.");
        }

        // Constructor validation applies to the final collection when a property repeats.
        bool hasNullElement = false;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return !hasNullElement;
            }

            hasNullElement |= JsonSerializer.Deserialize<T>(ref reader, options) is null;
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
}
