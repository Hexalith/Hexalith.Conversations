// <copyright file="ProviderCorrelationMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Stores opaque provider correlation metadata that is never authoritative identity.
/// </summary>
/// <param name="providerName">The provider name as safe correlation metadata.</param>
/// <param name="providerType">The provider category as safe correlation metadata.</param>
/// <param name="metadataSchemaVersion">The schema version for the provider metadata extension.</param>
/// <param name="providerSessionReference">An optional provider session reference used only for correlation.</param>
/// <param name="providerResponseReference">An optional provider response reference used only for correlation.</param>
/// <param name="extensionData">A bounded opaque extension bag with safe string values.</param>
public sealed record ProviderCorrelationMetadata(
    string ProviderName,
    string ProviderType,
    SchemaVersion MetadataSchemaVersion,
    string? ProviderSessionReference = null,
    string? ProviderResponseReference = null,
    IReadOnlyDictionary<string, string>? ExtensionData = null)
{
    /// <summary>
    /// Gets the provider name as safe correlation metadata.
    /// </summary>
    public string ProviderName { get; } = ValidateRequired(ProviderName);

    /// <summary>
    /// Gets the provider category as safe correlation metadata.
    /// </summary>
    public string ProviderType { get; } = ValidateRequired(ProviderType);

    private static string ValidateRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
