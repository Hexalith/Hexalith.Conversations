// <copyright file="IdentifierJsonConverters.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ConversationIdJsonConverter : PrefixedIdentifierJsonConverter<ConversationId>
{
    protected override string Prefix => "conv";

    protected override ConversationId Create(string value) => new(value);

    protected override string GetValue(ConversationId value) => value.Value;
}

internal sealed class TenantIdJsonConverter : PrefixedIdentifierJsonConverter<TenantId>
{
    protected override string Prefix => "tenant";

    protected override TenantId Create(string value) => new(value);

    protected override string GetValue(TenantId value) => value.Value;
}

internal sealed class PartyIdJsonConverter : PrefixedIdentifierJsonConverter<PartyId>
{
    protected override string Prefix => "party";

    protected override PartyId Create(string value) => new(value);

    protected override string GetValue(PartyId value) => value.Value;
}

internal sealed class ProjectIdJsonConverter : PrefixedIdentifierJsonConverter<ProjectId>
{
    protected override string Prefix => "project";

    protected override ProjectId Create(string value) => new(value);

    protected override string GetValue(ProjectId value) => value.Value;
}

internal sealed class FolderIdJsonConverter : PrefixedIdentifierJsonConverter<FolderId>
{
    protected override string Prefix => "folder";

    protected override FolderId Create(string value) => new(value);

    protected override string GetValue(FolderId value) => value.Value;
}

internal sealed class FileIdJsonConverter : PrefixedIdentifierJsonConverter<FileId>
{
    protected override string Prefix => "file";

    protected override FileId Create(string value) => new(value);

    protected override string GetValue(FileId value) => value.Value;
}

internal sealed class MessageIdJsonConverter : PrefixedIdentifierJsonConverter<MessageId>
{
    protected override string Prefix => "message";

    protected override MessageId Create(string value) => new(value);

    protected override string GetValue(MessageId value) => value.Value;
}
