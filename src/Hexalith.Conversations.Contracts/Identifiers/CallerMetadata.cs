// <copyright file="CallerMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Identifiers;

/// <summary>
/// Carries bounded, content-safe caller-supplied provenance metadata for attribution, audit, and composition.
/// </summary>
/// <remarks>
/// <para>
/// Caller metadata is <b>provenance only</b>. It is never authorization, tenant truth, governance truth, or
/// UI-inferred trust state, and it never substitutes for the claims-derived tenant binding decided by the local
/// Tenants projection. Approved fields are limited to non-personal, non-transcript, non-derived-authority,
/// non-secret values useful for routing, correlation, lifecycle, or operational diagnosis.
/// </para>
/// <para>
/// Correlation and causation identifiers are NOT duplicated here; they remain first-class on
/// <see cref="Commands.ConversationCommandMetadata"/> and <see cref="Events.ConversationEventMetadata"/>.
/// Tenant identity, Party identity, tokens, claims, provider payloads, raw prompts, message/redacted text, and
/// protected content are forbidden as caller-metadata keys or values and are rejected by construction through the
/// shared <see cref="ConversationError"/> content-safety guardrail and the bounded size/count caps below.
/// </para>
/// <para>
/// Policy split (AC3): malformed, oversized, unbounded, sensitive, or unsupported metadata is <b>rejected</b> at
/// construction (and re-bounded at the command boundary) rather than silently truncated, because truncating a
/// content-unsafe value cannot guarantee a safe residual fragment.
/// </para>
/// </remarks>
/// <param name="metadataSchemaVersion">The schema version for the caller metadata extension.</param>
/// <param name="clientName">An optional safe caller client name.</param>
/// <param name="clientVersion">An optional safe caller client version.</param>
/// <param name="composerSource">An optional safe composer source.</param>
/// <param name="origin">An optional safe caller origin.</param>
/// <param name="integrationContext">An optional safe integration context.</param>
/// <param name="extensionData">A bounded opaque extension bag with safe string values.</param>
public sealed record CallerMetadata(
    SchemaVersion MetadataSchemaVersion,
    string? ClientName = null,
    string? ClientVersion = null,
    string? ComposerSource = null,
    string? Origin = null,
    string? IntegrationContext = null,
    IReadOnlyDictionary<string, string>? ExtensionData = null)
{
    /// <summary>
    /// The maximum allowed length of any single caller-metadata field or extension key/value.
    /// </summary>
    public const int ValueMaxLength = 256;

    /// <summary>
    /// The maximum allowed number of extension entries.
    /// </summary>
    public const int ExtensionEntryMaxCount = 32;

    /// <summary>
    /// Gets the schema version for the caller metadata extension.
    /// </summary>
    public SchemaVersion MetadataSchemaVersion { get; } = RequireNonNull(MetadataSchemaVersion, nameof(MetadataSchemaVersion));

    /// <summary>
    /// Gets the optional safe caller client name.
    /// </summary>
    public string? ClientName { get; } = ValidateField(ClientName, nameof(ClientName));

    /// <summary>
    /// Gets the optional safe caller client version.
    /// </summary>
    public string? ClientVersion { get; } = ValidateField(ClientVersion, nameof(ClientVersion));

    /// <summary>
    /// Gets the optional safe composer source.
    /// </summary>
    public string? ComposerSource { get; } = ValidateField(ComposerSource, nameof(ComposerSource));

    /// <summary>
    /// Gets the optional safe caller origin.
    /// </summary>
    public string? Origin { get; } = ValidateField(Origin, nameof(Origin));

    /// <summary>
    /// Gets the optional safe integration context.
    /// </summary>
    public string? IntegrationContext { get; } = ValidateField(IntegrationContext, nameof(IntegrationContext));

    /// <summary>
    /// Gets the bounded opaque extension bag.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ExtensionData { get; } = ValidateExtensionData(ExtensionData);

    /// <summary>
    /// Evaluates whether a caller-metadata instance stays within the bounded size/count caps without throwing.
    /// </summary>
    /// <remarks>
    /// Construction already enforces these caps and the content-safety guardrail, so this method is the boundary
    /// re-check used by command validation to return a typed rejection rather than an exception. It returns a stable
    /// bounded reason code (never raw caller text) when a bound is exceeded.
    /// </remarks>
    /// <param name="metadata">The caller metadata to evaluate.</param>
    /// <param name="reasonCode">The bounded machine-readable reason code when the metadata is out of bounds.</param>
    /// <returns><see langword="true"/> when the metadata is within bounds; otherwise <see langword="false"/>.</returns>
    public static bool TryValidateBounds(CallerMetadata? metadata, out string? reasonCode)
    {
        reasonCode = null;
        if (metadata is null)
        {
            return true;
        }

        if (!IsFieldWithinBounds(metadata.ClientName)
            || !IsFieldWithinBounds(metadata.ClientVersion)
            || !IsFieldWithinBounds(metadata.ComposerSource)
            || !IsFieldWithinBounds(metadata.Origin)
            || !IsFieldWithinBounds(metadata.IntegrationContext))
        {
            reasonCode = "caller_metadata_invalid";
            return false;
        }

        return TryValidateMetadataBag(metadata.ExtensionData, out reasonCode);
    }

    /// <summary>
    /// Evaluates whether a bounded opaque string metadata bag stays within the caller-metadata caps without throwing.
    /// </summary>
    /// <remarks>
    /// Reused by command validation to bound the existing safe adopter metadata bag
    /// (<see cref="Commands.UpdateConversationMetadataCommand.Attributes"/>) with the same deterministic policy.
    /// </remarks>
    /// <param name="attributes">The metadata bag to evaluate.</param>
    /// <param name="reasonCode">The bounded machine-readable reason code when the bag is out of bounds.</param>
    /// <returns><see langword="true"/> when the bag is within bounds; otherwise <see langword="false"/>.</returns>
    public static bool TryValidateMetadataBag(IReadOnlyDictionary<string, string>? attributes, out string? reasonCode)
    {
        reasonCode = null;
        if (attributes is null)
        {
            return true;
        }

        if (attributes.Count > ExtensionEntryMaxCount)
        {
            reasonCode = "caller_metadata_too_many_entries";
            return false;
        }

        foreach (KeyValuePair<string, string> entry in attributes)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)
                || entry.Value is null
                || !IsFieldWithinBounds(entry.Key)
                || !IsFieldWithinBounds(entry.Value)
                || !IsContentSafe(entry.Key)
                || !IsContentSafe(entry.Value))
            {
                reasonCode = "caller_metadata_invalid";
                return false;
            }
        }

        return true;
    }

    private static string? ValidateField(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!IsFieldWithinBounds(value))
        {
            throw new ArgumentException(
                $"Caller metadata field must be at most {ValueMaxLength} characters and contain no control characters.",
                parameterName);
        }

        // Reuse the shared free-text content-safety blocklist so caller metadata cannot smuggle tenant/Party/provider
        // payload/secret/local-path/exception fragments into attribution, audit, projection, or composition surfaces.
        ConversationError.EnsureContentSafe(value, parameterName);
        return value;
    }

    private static IReadOnlyDictionary<string, string>? ValidateExtensionData(IReadOnlyDictionary<string, string>? extensionData)
    {
        if (extensionData is null)
        {
            return null;
        }

        if (extensionData.Count > ExtensionEntryMaxCount)
        {
            throw new ArgumentException(
                $"Caller metadata extension data must contain at most {ExtensionEntryMaxCount} entries.",
                nameof(extensionData));
        }

        foreach (KeyValuePair<string, string> entry in extensionData)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new ArgumentException("Caller metadata extension keys must be non-empty.", nameof(extensionData));
            }

            if (entry.Value is null)
            {
                throw new ArgumentException($"Caller metadata extension value for key '{entry.Key}' must not be null.", nameof(extensionData));
            }

            if (!IsFieldWithinBounds(entry.Key) || !IsFieldWithinBounds(entry.Value))
            {
                throw new ArgumentException(
                    $"Caller metadata extension keys and values must be at most {ValueMaxLength} characters and contain no control characters.",
                    nameof(extensionData));
            }

            ConversationError.EnsureContentSafe(entry.Key, nameof(extensionData));
            ConversationError.EnsureContentSafe(entry.Value, nameof(extensionData));
        }

        return extensionData;
    }

    private static bool IsFieldWithinBounds(string? value)
    {
        if (value is null)
        {
            return true;
        }

        return value.Length <= ValueMaxLength && !ContainsControlCharacter(value);
    }

    private static bool IsContentSafe(string value)
    {
        try
        {
            ConversationError.EnsureContentSafe(value, "value");
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char c in value)
        {
            if (char.IsControl(c))
            {
                return true;
            }
        }

        return false;
    }

    private static T RequireNonNull<T>(T value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);
}
