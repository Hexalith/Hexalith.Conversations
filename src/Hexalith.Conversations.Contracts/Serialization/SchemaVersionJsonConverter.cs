// <copyright file="SchemaVersionJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class SchemaVersionJsonConverter : ConversationIntValueJsonConverter<SchemaVersion>
{
    protected override SchemaVersion Create(int value) => new(value);

    protected override int GetValue(SchemaVersion value) => value.Value;
}
