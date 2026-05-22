// <copyright file="ContractCompatibilityMetadata.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Versioning;

/// <summary>
/// Defines the closed adopter-facing compatibility status vocabulary.
/// </summary>
[JsonConverter(typeof(ContractCompatibilityStatusJsonConverter))]
public sealed record ContractCompatibilityStatus
{
    /// <summary>
    /// Gets the status for actively supported versions.
    /// </summary>
    public static ContractCompatibilityStatus Supported { get; } = new("supported");

    /// <summary>
    /// Gets the status for accepted versions that should be upgraded.
    /// </summary>
    public static ContractCompatibilityStatus Deprecated { get; } = new("deprecated");

    /// <summary>
    /// Gets the status for recognized but unsupported versions.
    /// </summary>
    public static ContractCompatibilityStatus Unsupported { get; } = new("unsupported");

    /// <summary>
    /// Gets the status for malformed or missing version input.
    /// </summary>
    public static ContractCompatibilityStatus Invalid { get; } = new("invalid");

    private static readonly IReadOnlyDictionary<string, ContractCompatibilityStatus> KnownStatuses =
        new[]
        {
            Supported,
            Deprecated,
            Unsupported,
            Invalid,
        }.ToDictionary(status => status.Value, StringComparer.Ordinal);

    private ContractCompatibilityStatus(string value) => Value = value;

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported compatibility status. Matching is case-sensitive.
    /// </summary>
    /// <param name="value">The canonical status value.</param>
    /// <returns>The matching compatibility status.</returns>
    public static ContractCompatibilityStatus Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownStatuses.TryGetValue(value, out ContractCompatibilityStatus? status)
            ? status
            : throw new ArgumentException($"Unsupported contract compatibility status '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Describes a NuGet package version exposed in compatibility metadata.
/// </summary>
/// <param name="PackageId">The public package identifier.</param>
/// <param name="Version">The semantic package version.</param>
public sealed partial record ContractPackageVersionInfo(string PackageId, string Version)
{
    /// <summary>
    /// Gets the public package identifier.
    /// </summary>
    public string PackageId { get; } = EnsureRequired(PackageId, nameof(PackageId));

    /// <summary>
    /// Gets the semantic package version.
    /// </summary>
    public string Version { get; } = EnsureSemanticVersion(Version, nameof(Version));

    internal static string EnsureSemanticVersion(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!SemanticVersionRegex().IsMatch(value))
        {
            throw new ArgumentException("Package version must be a semantic version.", parameterName);
        }

        return value;
    }

    private static string EnsureRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    [GeneratedRegex(@"^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z][0-9A-Za-z.-]*)?$", RegexOptions.CultureInvariant)]
    internal static partial Regex SemanticVersionRegex();
}

/// <summary>
/// Provides a bounded remediation pointer for deprecated, unsupported, or invalid compatibility checks.
/// </summary>
/// <param name="GuidanceCode">The bounded machine-readable guidance code.</param>
/// <param name="DocumentationUri">The safe public documentation pointer.</param>
public sealed record ContractCompatibilityRemediation(string GuidanceCode, Uri DocumentationUri)
{
    private static readonly IReadOnlySet<string> KnownGuidanceCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "upgrade-to-active-v1",
        "use-supported-v1-package",
        "send-positive-integer-schema-version",
    };

    /// <summary>
    /// Gets the bounded machine-readable guidance code.
    /// </summary>
    public string GuidanceCode { get; } = EnsureKnownGuidanceCode(GuidanceCode);

    /// <summary>
    /// Gets the safe public documentation pointer.
    /// </summary>
    public Uri DocumentationUri { get; } = EnsureDocumentationUri(DocumentationUri);

    private static string EnsureKnownGuidanceCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownGuidanceCodes.Contains(value)
            ? value
            : throw new ArgumentException($"Unsupported compatibility guidance code '{value}'.", nameof(GuidanceCode));
    }

    private static Uri EnsureDocumentationUri(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.IsAbsoluteUri || value.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Compatibility documentation pointers must use absolute HTTPS URIs.", nameof(DocumentationUri));
        }

        return value;
    }
}

/// <summary>
/// Describes active adopter-facing contract and package compatibility metadata.
/// </summary>
/// <param name="SchemaVersion">The metadata schema version.</param>
/// <param name="Status">The aggregate active compatibility status.</param>
/// <param name="CommandContracts">The command contract family version metadata.</param>
/// <param name="ProjectionContracts">The projection contract family version metadata.</param>
/// <param name="EventContracts">The event contract family version metadata.</param>
/// <param name="ContractsPackage">The contracts package version metadata.</param>
/// <param name="ClientPackage">The supported .NET client package version metadata.</param>
/// <param name="Remediations">Safe remediation pointers when the active status is not supported.</param>
public sealed record ContractCompatibilityMetadata(
    SchemaVersion SchemaVersion,
    ContractCompatibilityStatus Status,
    ContractVersionInfo CommandContracts,
    ContractVersionInfo ProjectionContracts,
    ContractVersionInfo EventContracts,
    ContractPackageVersionInfo ContractsPackage,
    ContractPackageVersionInfo ClientPackage,
    IReadOnlyList<ContractCompatibilityRemediation> Remediations)
{
    /// <summary>
    /// Gets the metadata schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the aggregate active compatibility status.
    /// </summary>
    public ContractCompatibilityStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    /// <summary>
    /// Gets command contract family version metadata.
    /// </summary>
    public ContractVersionInfo CommandContracts { get; } = CommandContracts ?? throw new ArgumentNullException(nameof(CommandContracts));

    /// <summary>
    /// Gets projection contract family version metadata.
    /// </summary>
    public ContractVersionInfo ProjectionContracts { get; } = ProjectionContracts ?? throw new ArgumentNullException(nameof(ProjectionContracts));

    /// <summary>
    /// Gets event contract family version metadata.
    /// </summary>
    public ContractVersionInfo EventContracts { get; } = EventContracts ?? throw new ArgumentNullException(nameof(EventContracts));

    /// <summary>
    /// Gets contracts package version metadata.
    /// </summary>
    public ContractPackageVersionInfo ContractsPackage { get; } = ContractsPackage ?? throw new ArgumentNullException(nameof(ContractsPackage));

    /// <summary>
    /// Gets supported .NET client package version metadata.
    /// </summary>
    public ContractPackageVersionInfo ClientPackage { get; } = ClientPackage ?? throw new ArgumentNullException(nameof(ClientPackage));

    /// <summary>
    /// Gets safe remediation pointers when active metadata is not supported.
    /// </summary>
    public IReadOnlyList<ContractCompatibilityRemediation> Remediations { get; } = ValidateRemediations(Status, Remediations);

    private static IReadOnlyList<ContractCompatibilityRemediation> ValidateRemediations(
        ContractCompatibilityStatus status,
        IReadOnlyList<ContractCompatibilityRemediation>? remediations)
    {
        ArgumentNullException.ThrowIfNull(status);
        ContractCompatibilityRemediation[] validated = ValidateRemediationItems(remediations);

        if (status == ContractCompatibilityStatus.Supported && validated.Length > 0)
        {
            throw new ArgumentException("Supported compatibility metadata must not include remediation pointers.", nameof(Remediations));
        }

        if (status != ContractCompatibilityStatus.Supported && validated.Length == 0)
        {
            throw new ArgumentException("Non-supported compatibility metadata must include at least one remediation pointer.", nameof(Remediations));
        }

        return validated;
    }

    internal static ContractCompatibilityRemediation[] ValidateRemediationItems(
        IReadOnlyList<ContractCompatibilityRemediation>? remediations)
    {
        if (remediations is null)
        {
            return [];
        }

        ContractCompatibilityRemediation[] validated = new ContractCompatibilityRemediation[remediations.Count];
        for (int i = 0; i < remediations.Count; i++)
        {
            validated[i] = remediations[i]
                ?? throw new ArgumentException("Compatibility remediation lists cannot contain null entries.", nameof(remediations));
        }

        return validated;
    }
}

/// <summary>
/// Carries adopter-supplied version values for a compatibility check.
/// </summary>
/// <param name="CommandSchemaVersion">The requested command schema version.</param>
/// <param name="ProjectionSchemaVersion">The requested projection schema version.</param>
/// <param name="EventSchemaVersion">The requested event schema version.</param>
/// <param name="ContractsPackageVersion">The requested contracts package version.</param>
/// <param name="ClientPackageVersion">The requested .NET client package version.</param>
public sealed record ContractCompatibilityRequest(
    string? CommandSchemaVersion = null,
    string? ProjectionSchemaVersion = null,
    string? EventSchemaVersion = null,
    string? ContractsPackageVersion = null,
    string? ClientPackageVersion = null);

/// <summary>
/// Reports the content-safe result of a compatibility check.
/// </summary>
/// <param name="SchemaVersion">The result schema version.</param>
/// <param name="Status">The compatibility status.</param>
/// <param name="ActiveMetadata">The active compatibility metadata.</param>
/// <param name="Remediations">Safe remediation pointers when the status is not supported.</param>
/// <param name="Error">Optional typed error for invalid or unsupported checks.</param>
public sealed record ContractCompatibilityResult(
    SchemaVersion SchemaVersion,
    ContractCompatibilityStatus Status,
    ContractCompatibilityMetadata ActiveMetadata,
    IReadOnlyList<ContractCompatibilityRemediation> Remediations,
    ConversationError? Error = null)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the compatibility status.
    /// </summary>
    public ContractCompatibilityStatus Status { get; } = Status ?? throw new ArgumentNullException(nameof(Status));

    /// <summary>
    /// Gets active compatibility metadata.
    /// </summary>
    public ContractCompatibilityMetadata ActiveMetadata { get; } = ActiveMetadata ?? throw new ArgumentNullException(nameof(ActiveMetadata));

    /// <summary>
    /// Gets safe remediation pointers when the status is not supported.
    /// </summary>
    public IReadOnlyList<ContractCompatibilityRemediation> Remediations { get; } = ValidateResultRemediations(Status, Remediations);

    /// <summary>
    /// Gets the optional typed error for invalid or unsupported checks.
    /// </summary>
    public ConversationError? Error { get; } = ValidateError(Status, Error);

    private static IReadOnlyList<ContractCompatibilityRemediation> ValidateResultRemediations(
        ContractCompatibilityStatus status,
        IReadOnlyList<ContractCompatibilityRemediation>? remediations)
    {
        ArgumentNullException.ThrowIfNull(status);
        ContractCompatibilityRemediation[] validated = ContractCompatibilityMetadata.ValidateRemediationItems(remediations);

        if (status == ContractCompatibilityStatus.Supported && validated.Length > 0)
        {
            throw new ArgumentException("Supported compatibility results must not include remediation pointers.", nameof(Remediations));
        }

        if (status != ContractCompatibilityStatus.Supported && validated.Length == 0)
        {
            throw new ArgumentException("Non-supported compatibility results must include at least one remediation pointer.", nameof(Remediations));
        }

        return validated;
    }

    private static ConversationError? ValidateError(ContractCompatibilityStatus status, ConversationError? error)
    {
        ArgumentNullException.ThrowIfNull(status);
        if ((status == ContractCompatibilityStatus.Invalid || status == ContractCompatibilityStatus.Unsupported) && error is null)
        {
            throw new ArgumentException("Invalid and unsupported compatibility results must include a typed versioning error.", nameof(Error));
        }

        if ((status == ContractCompatibilityStatus.Supported || status == ContractCompatibilityStatus.Deprecated) && error is not null)
        {
            throw new ArgumentException("Supported and deprecated compatibility results must not include a typed failure error.", nameof(Error));
        }

        return error;
    }
}

/// <summary>
/// Provides the active Conversations contract compatibility metadata and safe version checks.
/// </summary>
public static class ConversationContractCompatibility
{
    private static readonly Uri CompatibilityDocumentation =
        new("https://docs.hexalith.local/conversations/contracts/v1/compatibility", UriKind.Absolute);

    private static readonly IReadOnlySet<string> DeprecatedPackageVersions = new HashSet<string>(StringComparer.Ordinal)
    {
        "0.9.0",
    };

    /// <summary>
    /// Gets the active contract compatibility metadata.
    /// </summary>
    public static ContractCompatibilityMetadata Current { get; } = new(
        SchemaVersion.Current,
        ContractCompatibilityStatus.Supported,
        new ContractVersionInfo("commands", SchemaVersion.Current, SchemaVersion.Current),
        new ContractVersionInfo("projections", SchemaVersion.Current, SchemaVersion.Current),
        new ContractVersionInfo("events", SchemaVersion.Current, SchemaVersion.Current),
        new ContractPackageVersionInfo("Hexalith.Conversations.Contracts", "1.0.0"),
        new ContractPackageVersionInfo("Hexalith.Conversations.Client", "1.0.0"),
        []);

    /// <summary>
    /// Evaluates requested versions against the active v1 compatibility metadata.
    /// </summary>
    /// <param name="request">The requested versions to evaluate.</param>
    /// <returns>A content-safe machine-readable compatibility result.</returns>
    public static ContractCompatibilityResult Evaluate(ContractCompatibilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ContractCompatibilityRemediation> remediations = [];
        Dictionary<string, string> diagnostics = new(StringComparer.Ordinal);

        CompatibilitySeverity severity = CompatibilitySeverity.Supported;
        EvaluateSchemaVersion(request.CommandSchemaVersion, "commandSchemaVersion", diagnostics, ref severity);
        EvaluateSchemaVersion(request.ProjectionSchemaVersion, "projectionSchemaVersion", diagnostics, ref severity);
        EvaluateSchemaVersion(request.EventSchemaVersion, "eventSchemaVersion", diagnostics, ref severity);
        EvaluatePackageVersion(request.ContractsPackageVersion, Current.ContractsPackage.Version, "contractsPackageVersion", diagnostics, ref severity);
        EvaluatePackageVersion(request.ClientPackageVersion, Current.ClientPackage.Version, "clientPackageVersion", diagnostics, ref severity);

        ContractCompatibilityStatus status = StatusFor(severity);
        if (status != ContractCompatibilityStatus.Supported)
        {
            remediations.Add(RemediationFor(severity));
        }

        ConversationError? error = severity is CompatibilitySeverity.Invalid or CompatibilitySeverity.Unsupported
            ? CreateVersioningError(diagnostics, severity)
            : null;

        return new ContractCompatibilityResult(SchemaVersion.Current, status, Current, remediations, error);
    }

    private static void EvaluateSchemaVersion(
        string? requested,
        string fieldName,
        IDictionary<string, string> diagnostics,
        ref CompatibilitySeverity severity)
    {
        if (requested is null)
        {
            return;
        }

        if (!int.TryParse(requested, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 1)
        {
            diagnostics[fieldName] = "invalid_positive_integer_required";
            RaiseSeverity(ref severity, CompatibilitySeverity.Invalid);
            return;
        }

        if (value > SchemaVersion.Current.Value)
        {
            diagnostics[fieldName] = "unsupported_schema_version";
            RaiseSeverity(ref severity, CompatibilitySeverity.Unsupported);
        }
    }

    private static void EvaluatePackageVersion(
        string? requested,
        string activeVersion,
        string fieldName,
        IDictionary<string, string> diagnostics,
        ref CompatibilitySeverity severity)
    {
        if (requested is null)
        {
            return;
        }

        if (!ContractPackageVersionInfo.SemanticVersionRegex().IsMatch(requested))
        {
            diagnostics[fieldName] = "invalid_semantic_version_required";
            RaiseSeverity(ref severity, CompatibilitySeverity.Invalid);
            return;
        }

        if (requested == activeVersion)
        {
            return;
        }

        if (DeprecatedPackageVersions.Contains(requested))
        {
            diagnostics[fieldName] = "deprecated_package_version";
            RaiseSeverity(ref severity, CompatibilitySeverity.Deprecated);
            return;
        }

        diagnostics[fieldName] = "unsupported_package_version";
        RaiseSeverity(ref severity, CompatibilitySeverity.Unsupported);
    }

    private static ConversationError CreateVersioningError(IReadOnlyDictionary<string, string> diagnostics, CompatibilitySeverity severity)
        => new(
            SchemaVersion.Current,
            ConversationErrorCode.SchemaVersionUnsupported,
            ConversationErrorCategory.Versioning,
            IsRetryable: false,
            CorrelationId: "compatibility-check",
            Documentation: CompatibilityDocumentation,
            SafeFieldDiagnostics: diagnostics,
            DeveloperGuidance: severity == CompatibilitySeverity.Invalid
                ? "Send positive integer schema versions and semantic package versions."
                : "Use the active v1 contracts package and client package.");

    private static ContractCompatibilityRemediation RemediationFor(CompatibilitySeverity severity)
        => severity switch
        {
            CompatibilitySeverity.Invalid => new("send-positive-integer-schema-version", CompatibilityDocumentation),
            CompatibilitySeverity.Deprecated => new("upgrade-to-active-v1", CompatibilityDocumentation),
            CompatibilitySeverity.Unsupported => new("use-supported-v1-package", CompatibilityDocumentation),
            _ => new("upgrade-to-active-v1", CompatibilityDocumentation),
        };

    private static ContractCompatibilityStatus StatusFor(CompatibilitySeverity severity)
        => severity switch
        {
            CompatibilitySeverity.Invalid => ContractCompatibilityStatus.Invalid,
            CompatibilitySeverity.Unsupported => ContractCompatibilityStatus.Unsupported,
            CompatibilitySeverity.Deprecated => ContractCompatibilityStatus.Deprecated,
            _ => ContractCompatibilityStatus.Supported,
        };

    private static void RaiseSeverity(ref CompatibilitySeverity current, CompatibilitySeverity candidate)
    {
        if (candidate > current)
        {
            current = candidate;
        }
    }

    private enum CompatibilitySeverity
    {
        Supported = 0,
        Deprecated = 1,
        Unsupported = 2,
        Invalid = 3,
    }
}
