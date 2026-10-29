// <copyright file="ConversationDeletionRuntimeOptions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Explicit opt-in installation references for the deletion runtime host. Configuration is never authority or enrollment.</summary>
/// <remarks>Credentials are not part of these options. The worker uses the SDK gateway authority transport
/// (<c>EventStore:Authentication</c>); independent lookups use the separately configured <see cref="LookupIssuerSectionName"/> issuer.</remarks>
public sealed class ConversationDeletionRuntimeOptions
{
    /// <summary>The configuration section holding these options.</summary>
    public const string SectionName = "Hexalith:Conversations:DeletionRuntime";

    /// <summary>The explicit opt-in key; absent or false keeps the default fail-closed host.</summary>
    public const string EnabledKey = SectionName + ":Enabled";

    /// <summary>The section binding the separate lookup workload-assertion issuer.</summary>
    public const string LookupIssuerSectionName = SectionName + ":LookupIssuer";

    /// <summary>The installed Conversation source feed served by the runtime.</summary>
    public const string FeedName = "approved-deletion-v1";

    /// <summary>The isolated receiver domain; never an Agents domain.</summary>
    public const string ReceiverDomain = "conversation-deletion-conformance";

    /// <summary>Gets or sets a value indicating whether the host explicitly opts in.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the only installed tenant.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the enrolled worker principal, which is the gateway token subject for background delivery.</summary>
    public string WorkerPrincipalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the separately enrolled dedicated Conversations service Party.</summary>
    public string ServicePartyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the independently installed source namespace coverage identity.</summary>
    public string NamespaceInstallationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the installed Conversations EventStore storage reference, containing no credentials.</summary>
    public string StorageIdentity { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact HTTPS, or loopback HTTP, EventStore gateway origin.</summary>
    public string GatewayBaseAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the EventStore platform workload that binds admitted complete command sources.</summary>
    public string PlatformWorkloadId { get; set; } = string.Empty;

    /// <summary>Gets or sets the receiver principal, which is also its Dapr application id and lookup assertion audience.</summary>
    public string ReceiverPrincipalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the separately enrolled independent lookup workload, sent as the query user.</summary>
    public string LookupPrincipalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the only authenticated principal allowed to read the runtime identity.</summary>
    public string IdentityReaderPrincipalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the full immutable receiver targets this source accepts, including a pinned rollover target.</summary>
    public string[] AcceptedTargetVersions { get; set; } = [];

    /// <summary>Gets the exact installed tenant identifier.</summary>
    internal TenantId Tenant => new(TenantId);

    /// <summary>Gets the exact installed source namespace.</summary>
    internal SourcePublicationScope Scope => new(TenantId, "conversation", FeedName, NamespaceInstallationId);

    /// <summary>Checks that a target is one of the installed full immutable receiver targets.</summary>
    /// <param name="target">The candidate target.</param>
    /// <returns>True only for an exact configured full target.</returns>
    public bool AcceptsTarget(string? target)
        => target is not null && IsFullTarget(target) && AcceptedTargetVersions.Contains(target, StringComparer.Ordinal);

    /// <summary>Requires a canonical lowercase 40-hex immutable commit.</summary>
    /// <param name="target">The candidate.</param>
    /// <returns>True for a full target.</returns>
    public static bool IsFullTarget(string? target) => target is { Length: 40 }
        && target.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>Lists invalid configuration keys without echoing any value.</summary>
    /// <returns>The invalid keys; empty when complete.</returns>
    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();
        if (!Enabled)
        {
            failures.Add(nameof(Enabled));
        }

        if (!Try(() => new AggregateIdentity(TenantId, "conversation", "identity").TenantId == TenantId
            && new AggregateIdentity(TenantId, ReceiverDomain, "identity").TenantId == TenantId))
        {
            failures.Add(nameof(TenantId));
        }

        if (!Try(() => new PartyId(ServicePartyId).Value == ServicePartyId.Trim() && ServicePartyId.Length <= 256))
        {
            failures.Add(nameof(ServicePartyId));
        }

        if (!Try(() => Scope.InstallationId == NamespaceInstallationId))
        {
            failures.Add(nameof(NamespaceInstallationId));
        }

        if (!Bounded(StorageIdentity))
        {
            failures.Add(nameof(StorageIdentity));
        }

        (string Name, string Value)[] principals =
        [
            (nameof(WorkerPrincipalId), WorkerPrincipalId), (nameof(PlatformWorkloadId), PlatformWorkloadId),
            (nameof(ReceiverPrincipalId), ReceiverPrincipalId), (nameof(LookupPrincipalId), LookupPrincipalId),
            (nameof(IdentityReaderPrincipalId), IdentityReaderPrincipalId),
        ];
        foreach ((string name, string value) in principals)
        {
            if (!Bounded(value))
            {
                failures.Add(name);
            }
        }

        // The SDK complete-source registration bounds the platform workload at 200 characters.
        if (PlatformWorkloadId?.Length > 200 && !failures.Contains(nameof(PlatformWorkloadId)))
        {
            failures.Add(nameof(PlatformWorkloadId));
        }

        if (principals.Select(static principal => principal.Value).Distinct(StringComparer.Ordinal).Count() != principals.Length)
        {
            failures.Add("DistinctPrincipals");
        }

        if (!Try(() => new AggregateIdentity(TenantId, ReceiverDomain, ReceiverPrincipalId).AggregateId == ReceiverPrincipalId))
        {
            failures.Add(nameof(ReceiverPrincipalId) + ":AppId");
        }

        if (!TryGetGateway(out _))
        {
            failures.Add(nameof(GatewayBaseAddress));
        }

        if (AcceptedTargetVersions is not { Length: > 0 } || !AcceptedTargetVersions.All(IsFullTarget)
            || AcceptedTargetVersions.Distinct(StringComparer.Ordinal).Count() != AcceptedTargetVersions.Length)
        {
            failures.Add(nameof(AcceptedTargetVersions));
        }

        return failures;
    }

    /// <summary>Gets the exact gateway origin.</summary>
    /// <param name="gateway">The validated origin.</param>
    /// <returns>True for an HTTPS or loopback HTTP origin without credentials, query, fragment or path.</returns>
    internal bool TryGetGateway(out Uri gateway)
    {
        gateway = null!;
        if (!Uri.TryCreate(GatewayBaseAddress, UriKind.Absolute, out Uri? uri) || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/"
            || !(uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
        {
            return false;
        }

        gateway = uri;
        return true;
    }

    private static bool Bounded(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && value == value.Trim() && !value.Any(char.IsControl);

    private static bool Try(Func<bool> check)
    {
        try
        {
            return check();
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
