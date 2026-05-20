// <copyright file="ConversationProviderCorrelationV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries sanitized provider correlation metadata that cannot authorize reads.
/// </summary>
/// <param name="providerName">The provider name as safe correlation metadata.</param>
/// <param name="providerType">The provider category as safe correlation metadata.</param>
/// <param name="metadataSchemaVersion">The provider metadata schema version.</param>
public sealed record ConversationProviderCorrelationV1(
    string ProviderName,
    string ProviderType,
    SchemaVersion MetadataSchemaVersion)
{
    /// <summary>
    /// Gets the provider name as safe correlation metadata.
    /// </summary>
    public string ProviderName { get; } = ValidateRequired(ProviderName, nameof(ProviderName));

    /// <summary>
    /// Gets the provider category as safe correlation metadata.
    /// </summary>
    public string ProviderType { get; } = ValidateRequired(ProviderType, nameof(ProviderType));

    /// <summary>
    /// Gets the provider metadata schema version.
    /// </summary>
    public SchemaVersion MetadataSchemaVersion { get; } =
        MetadataSchemaVersion ?? throw new ArgumentNullException(nameof(MetadataSchemaVersion));

    /// <summary>
    /// Creates sanitized public correlation metadata from a projection value.
    /// </summary>
    /// <param name="metadata">The projected provider correlation metadata.</param>
    /// <returns>The sanitized correlation metadata, or null when absent.</returns>
    public static ConversationProviderCorrelationV1? From(ProviderCorrelationMetadata? metadata)
        => metadata is null
            ? null
            : new(metadata.ProviderName, metadata.ProviderType, metadata.MetadataSchemaVersion);

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
