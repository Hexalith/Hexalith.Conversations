// <copyright file="ConversationCommandFingerprint.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;

namespace Hexalith.Conversations.Idempotency;

/// <summary>
/// Combines the scoped idempotency key with the canonical command fingerprint.
/// </summary>
/// <param name="Scope">The scoped idempotency key.</param>
/// <param name="PayloadFingerprint">The bounded canonical command fingerprint.</param>
public sealed record ConversationCommandFingerprint(
    ConversationIdempotencyScope Scope,
    ConversationPayloadFingerprint PayloadFingerprint)
{
    /// <summary>
    /// Gets the scoped idempotency key.
    /// </summary>
    public ConversationIdempotencyScope Scope { get; } = Scope ?? throw new ArgumentNullException(nameof(Scope));

    /// <summary>
    /// Gets the bounded canonical command fingerprint.
    /// </summary>
    public ConversationPayloadFingerprint PayloadFingerprint { get; } =
        PayloadFingerprint ?? throw new ArgumentNullException(nameof(PayloadFingerprint));

    /// <summary>
    /// Creates the fingerprint for a supported public command contract.
    /// </summary>
    /// <param name="command">The public command contract.</param>
    /// <param name="createAllocationScope">The Conversations-owned create allocation scope.</param>
    /// <returns>The scoped command fingerprint.</returns>
    public static ConversationCommandFingerprint Create(object command, ConversationId createAllocationScope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(createAllocationScope);

        return command switch
        {
            CreateConversationCommand create => CreateForCreateConversation(create, createAllocationScope),
            AppendMessageCommand append => CreateForAppendMessage(append),
            AddParticipantCommand add => CreateForAddParticipant(add),
            AttachFileReferenceCommand attach => CreateForAttachFileReference(attach),
            UpdateConversationMetadataCommand update => CreateForUpdateMetadata(update),
            CloseConversationCommand close => CreateForClose(close),
            ArchiveConversationCommand archive => CreateForArchive(archive),
            _ => throw new ArgumentException($"Unsupported conversation command type '{command.GetType().FullName}'.", nameof(command)),
        };
    }

    private static ConversationCommandFingerprint CreateForCreateConversation(
        CreateConversationCommand command,
        ConversationId allocationScope)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.CreateConversationCommand,
                ConversationIdempotencyScope.CreateAllocationScopeKind,
                allocationScope.Value),
            Fingerprint(
                MetadataParts(metadata)
                    .Concat(Optional("business.system", command.BusinessReference?.System))
                    .Concat(Optional("business.value", command.BusinessReference?.Value))
                    .Concat(Optional("project.id", command.ProjectId?.Value))
                    .Concat(Optional("folder.id", command.FolderId?.Value))
                    .Concat(Optional("label", command.Label))));
    }

    private static ConversationCommandFingerprint CreateForAppendMessage(AppendMessageCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.AppendMessageCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(
                MetadataParts(metadata)
                    .Concat(Required("message.id", command.MessageId?.Value))
                    .Concat(Required("author.party.id", command.AuthorPartyId?.Value))
                    .Concat(Required("text", command.Text))));
    }

    private static ConversationCommandFingerprint CreateForAddParticipant(AddParticipantCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.AddParticipantCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(
                MetadataParts(metadata)
                    .Concat(Required("participant.party.id", command.ParticipantPartyId?.Value))
                    .Concat(Required("participant.type", command.ParticipantType?.Value))
                    .Concat(Required("participant.role", command.ParticipantRole?.Value))));
    }

    private static ConversationCommandFingerprint CreateForAttachFileReference(AttachFileReferenceCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.AttachFileReferenceCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(
                MetadataParts(metadata)
                    .Concat(Required("file.id", command.FileId?.Value))
                    .Concat(Optional("folder.id", command.FolderId?.Value))
                    .Concat(Optional("message.id", command.MessageId?.Value))));
    }

    private static ConversationCommandFingerprint CreateForUpdateMetadata(UpdateConversationMetadataCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.UpdateConversationMetadataCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(
                MetadataParts(metadata)
                    .Concat(Optional("label", command.Label))
                    .Concat(Optional("business.system", command.BusinessReference?.System))
                    .Concat(Optional("business.value", command.BusinessReference?.Value))
                    .Concat(SafeAttributes(command.Attributes))));
    }

    private static ConversationCommandFingerprint CreateForClose(CloseConversationCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.CloseConversationCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(MetadataParts(metadata).Concat(Optional("reason.code", command.ReasonCode))));
    }

    private static ConversationCommandFingerprint CreateForArchive(ArchiveConversationCommand command)
    {
        ConversationCommandMetadata metadata = RequireMetadata(command.Metadata);
        return new ConversationCommandFingerprint(
            BuildScope(
                metadata,
                ConversationCommandType.ArchiveConversationCommand,
                ConversationIdempotencyScope.ConversationScopeKind,
                RequireNonNull(command.ConversationId, nameof(command.ConversationId)).Value),
            Fingerprint(MetadataParts(metadata).Concat(Optional("reason.code", command.ReasonCode))));
    }

    private static ConversationIdempotencyScope BuildScope(
        ConversationCommandMetadata metadata,
        ConversationCommandType commandType,
        string scopeKind,
        string scopeValue)
        => new(
            metadata.TenantId,
            commandType,
            scopeKind,
            scopeValue,

            // P9 review fix (2026-05-19): nameof previously misreported the parameter (the public entry point binds
            // 'command', not 'metadata'). Use a stable parameter label that matches the public surface.
            ValidateRequiredIdempotencyKey(metadata.IdempotencyKey),
            metadata.SchemaVersion);

    private static ConversationPayloadFingerprint Fingerprint(IEnumerable<KeyValuePair<string, string?>> parts)
        => ConversationPayloadFingerprint.FromParts(parts);

    private static IEnumerable<KeyValuePair<string, string?>> MetadataParts(ConversationCommandMetadata metadata)
    {
        yield return new KeyValuePair<string, string?>("actor.party.id", metadata.ActorPartyId.Value);
    }

    private static IEnumerable<KeyValuePair<string, string?>> SafeAttributes(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            yield break;
        }

        foreach (KeyValuePair<string, string> attribute in attributes.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            yield return new KeyValuePair<string, string?>($"attribute.{attribute.Key}", attribute.Value);
        }
    }

    private static IEnumerable<KeyValuePair<string, string?>> Required(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"Required canonical field '{key}' is missing or whitespace.",
                nameof(value));
        }

        return [new KeyValuePair<string, string?>(key, value)];
    }

    private static IEnumerable<KeyValuePair<string, string?>> Optional(string key, string? value)
    {
        if (value is not null)
        {
            yield return new KeyValuePair<string, string?>(key, value);
        }
    }

    private static ConversationCommandMetadata RequireMetadata(ConversationCommandMetadata? metadata)
        => metadata ?? throw new ArgumentNullException(nameof(metadata));

    private static string ValidateRequiredIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Conversations command idempotency key is required when constructing a scoped fingerprint.",
                paramName: "command.Metadata.IdempotencyKey");
        }

        return idempotencyKey;
    }

    private static T RequireNonNull<T>(T value, string parameterName) where T : class
        => value ?? throw new ArgumentNullException(parameterName);
}
