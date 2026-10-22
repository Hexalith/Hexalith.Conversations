using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Explicit installed enrollment references; configuration never authenticates a caller.</summary>
public sealed class DeletionConformanceOptions
{
    /// <summary>The required host configuration section.</summary>
    public const string SectionName = "DeletionConformance";
    /// <summary>Gets or sets explicit qualification-only activation.</summary>
    public bool Enabled { get; set; }
    /// <summary>Gets or sets the only permitted tenant.</summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>Gets or sets the installed immutable receiver commit.</summary>
    public string TargetVersion { get; set; } = string.Empty;
    /// <summary>Gets or sets the independently enrolled receiver workload/application identity.</summary>
    public string ReceiverPrincipalId { get; set; } = string.Empty;
    /// <summary>Gets or sets the separately enrolled lookup workload identity.</summary>
    public string LookupPrincipalId { get; set; } = string.Empty;
    /// <summary>Gets or sets the exact authenticated source actor the gateway places in command UserId.</summary>
    public string WorkerPrincipalId { get; set; } = string.Empty;
    /// <summary>Gets or sets the authenticated EventStore workload permitted to invoke processing.</summary>
    public string GatewayWorkloadId { get; set; } = string.Empty;
    /// <summary>Gets or sets the installed EventStore storage reference, containing no credentials.</summary>
    public string StorageIdentity { get; set; } = string.Empty;
    /// <summary>Gets or sets the authenticated EventStore gateway origin.</summary>
    public string GatewayBaseAddress { get; set; } = string.Empty;
    /// <summary>Gets or sets historical receiver targets retained for exact lost-acknowledgement lookup.</summary>
    public string[] RetainedTargetVersions { get; set; } = [];

    /// <summary>Checks required configuration without treating it as enrollment or runtime proof.</summary>
    public bool IsConfigured()
    {
        try
        {
            return Enabled && DeletionConformanceContract.ValidTarget(TargetVersion)
                && new AggregateIdentity(TenantId, DeletionConformanceContract.Domain, "identity").TenantId == TenantId
                && new[] { ReceiverPrincipalId, LookupPrincipalId, WorkerPrincipalId, GatewayWorkloadId, StorageIdentity }
                    .All(static value => !string.IsNullOrWhiteSpace(value) && value.Length <= 256)
                && new[] { ReceiverPrincipalId, LookupPrincipalId, WorkerPrincipalId, GatewayWorkloadId }.Distinct(StringComparer.Ordinal).Count() == 4
                && RetainedTargetVersions is not null && RetainedTargetVersions.All(DeletionConformanceContract.ValidTarget)
                && Uri.TryCreate(GatewayBaseAddress, UriKind.Absolute, out Uri? uri)
                && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
                && (uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback);
        }
        catch (ArgumentException) { return false; }
    }

    /// <summary>Allows only installed current or explicitly retained immutable targets.</summary>
    public bool AllowsTarget(string target) => target == TargetVersion || RetainedTargetVersions.Contains(target, StringComparer.Ordinal);
}
