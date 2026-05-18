// <copyright file="IdentifierJsonConverters.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ConversationIdJsonConverter : ConversationStringValueJsonConverter<ConversationId>
{
    protected override ConversationId Create(string value) => new(value);

    protected override string GetValue(ConversationId value) => value.Value;
}

internal sealed class TenantIdJsonConverter : ConversationStringValueJsonConverter<TenantId>
{
    protected override TenantId Create(string value) => new(value);

    protected override string GetValue(TenantId value) => value.Value;
}

internal sealed class PartyIdJsonConverter : ConversationStringValueJsonConverter<PartyId>
{
    protected override PartyId Create(string value) => new(value);

    protected override string GetValue(PartyId value) => value.Value;
}

internal sealed class ProjectIdJsonConverter : ConversationStringValueJsonConverter<ProjectId>
{
    protected override ProjectId Create(string value) => new(value);

    protected override string GetValue(ProjectId value) => value.Value;
}

internal sealed class FolderIdJsonConverter : ConversationStringValueJsonConverter<FolderId>
{
    protected override FolderId Create(string value) => new(value);

    protected override string GetValue(FolderId value) => value.Value;
}

internal sealed class FileIdJsonConverter : ConversationStringValueJsonConverter<FileId>
{
    protected override FileId Create(string value) => new(value);

    protected override string GetValue(FileId value) => value.Value;
}

internal sealed class MessageIdJsonConverter : ConversationStringValueJsonConverter<MessageId>
{
    protected override MessageId Create(string value) => new(value);

    protected override string GetValue(MessageId value) => value.Value;
}
