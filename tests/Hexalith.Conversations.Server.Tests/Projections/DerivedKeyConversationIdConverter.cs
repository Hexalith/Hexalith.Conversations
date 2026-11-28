// <copyright file="DerivedKeyConversationIdConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>Exercises the persisted projection readers with a configured non-string identifier converter.</summary>
internal sealed class DerivedKeyConversationIdConverter : JsonConverter<ConversationId>
{
    /// <inheritdoc/>
    public override ConversationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<string[]>(ref reader, options)!.Single());

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ConversationId value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Value);
        writer.WriteEndArray();
    }
}
