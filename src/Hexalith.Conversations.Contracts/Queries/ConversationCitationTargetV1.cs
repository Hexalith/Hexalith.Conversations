// <copyright file="ConversationCitationTargetV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Identifies the governed evidence entry requested for citation copy.
/// </summary>
public sealed record ConversationCitationTargetV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string EvidenceEntryId)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public string EvidenceEntryId { get; } = ValidateSafeToken(EvidenceEntryId, nameof(EvidenceEntryId));

    internal static string ValidateSafeToken(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Any(c => !IsSafeTokenCharacter(c))
            ? throw new ArgumentException("Citation target contains unsupported characters.", parameterName)
            : value;
    }

    private static bool IsSafeTokenCharacter(char value)
        => char.IsAsciiLetterOrDigit(value)
            || value is ':' or '-' or '_' or '.';
}
