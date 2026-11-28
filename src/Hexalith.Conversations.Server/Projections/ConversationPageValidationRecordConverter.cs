// <copyright file="ConversationPageValidationRecordConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Reads page validation fields with the store's serializer options.</summary>
internal sealed class ConversationPageValidationRecordConverter : JsonConverter<ConversationPageValidationRecord>
{
    /// <inheritdoc/>
    public override ConversationPageValidationRecord? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (ConversationProjectionJsonValidation.UsesStreamingPropertyBinding(options))
        {
            ConversationPageValidationFields? fields = JsonSerializer.Deserialize<ConversationPageValidationFields>(ref reader, options);
            return fields is null ? null : new ConversationPageValidationRecord(fields.Summary, fields.Detail, fields.DispatchId);
        }

        // Custom options apply to the persisted wrapper and its entire object graph. A separate
        // deserialization of Detail would lose references defined by preceding wrapper properties.
        ConversationProjectedReadModels? models = JsonSerializer.Deserialize<ConversationProjectedReadModels>(ref reader, options);
        return models is null ? null : ConversationPageValidationRecord.FromFullRecord(models);
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        ConversationPageValidationRecord value,
        JsonSerializerOptions options)
        => throw new NotSupportedException("Conversation page validation records are read-only.");
}
