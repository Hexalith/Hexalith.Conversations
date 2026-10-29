// <copyright file="ConversationRuntimeIdentityEndpoint.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Security.Claims;

using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Reports the authenticated installed runtime identity to the single configured reader.</summary>
/// <remarks>The tenant, namespace installation and storage identity are the installed references from configuration. The commit
/// (this build), receiver target (independent receiver lookup) and authority revision (current worker authority) are observed;
/// when any observed value is missing the response is unavailable rather than inferred.</remarks>
public static class ConversationRuntimeIdentityEndpoint
{
    /// <summary>The runtime identity route.</summary>
    public const string Route = "api/v1/ext-conv-ai-1/runtime-identity";

    /// <summary>Maps the runtime identity route; the caller supplies the authorization convention.</summary>
    /// <param name="endpoints">The route builder.</param>
    /// <returns>The route handler builder.</returns>
    public static RouteHandlerBuilder MapConversationRuntimeIdentity(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapGet(Route, static (HttpContext context, ConversationDeletionRuntimeOptions options,
            IConversationAgentAuthority authority, IConversationDeletionReceiver receiver, CancellationToken cancellationToken)
            => HandleAsync(context.User, options, authority, receiver, CommitOf(typeof(ServerAssemblyMarker).Assembly), cancellationToken));
    }

    /// <summary>Builds the identity response for one authenticated caller.</summary>
    /// <param name="user">The authenticated caller.</param>
    /// <param name="options">The installed references.</param>
    /// <param name="authority">Current independent authority.</param>
    /// <param name="receiver">The independent receiver adapter.</param>
    /// <param name="commit">The full build commit, if known.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>200 with the identity, 403 for any other caller, or 503 when a value cannot be established.</returns>
    internal static async Task<IResult> HandleAsync(ClaimsPrincipal user, ConversationDeletionRuntimeOptions options,
        IConversationAgentAuthority authority, IConversationDeletionReceiver receiver, string? commit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(receiver);
        cancellationToken.ThrowIfCancellationRequested();
        string[] subjects = [.. user.FindAll("sub").Select(static claim => claim.Value)];
        if (user.Identity?.IsAuthenticated != true || subjects.Length != 1 || subjects[0] != options.IdentityReaderPrincipalId)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (commit is null)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string? authorityRevision;
        string? receiverTarget;
        try
        {
            ConversationAgentAuthorization current = await authority.AuthorizeAsync(options.WorkerPrincipalId, options.Tenant, null,
                "DeletionSource", cancellationToken).ConfigureAwait(false);
            authorityRevision = ConversationAgentQueryService.CheckAuthority(current, options.WorkerPrincipalId, options.Tenant, true)
                == ConversationAgentsOutcome.Available && current.PartyId?.Value == options.ServicePartyId ? current.AuthorityRevision : null;
            receiverTarget = await receiver.CurrentTargetAsync(options.Tenant, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(authorityRevision) || !options.AcceptsTarget(receiverTarget))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new ConversationRuntimeIdentity(commit, options.TenantId, options.NamespaceInstallationId, receiverTarget!,
            options.StorageIdentity, authorityRevision), statusCode: StatusCodes.Status200OK);
    }

    /// <summary>Reads the full lowercase commit from an informational version <c>+&lt;40-hex&gt;</c> suffix.</summary>
    /// <param name="assembly">The built assembly.</param>
    /// <returns>The commit, or null when the build carries none.</returns>
    internal static string? CommitOf(Assembly assembly)
        => ParseCommit(assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Parses the full lowercase commit suffix.</summary>
    /// <param name="informationalVersion">The informational version.</param>
    /// <returns>The commit, or null.</returns>
    internal static string? ParseCommit(string? informationalVersion)
    {
        int separator = informationalVersion?.LastIndexOf('+') ?? -1;
        string? commit = separator < 0 ? null : informationalVersion![(separator + 1)..];
        return ConversationDeletionRuntimeOptions.IsFullTarget(commit) ? commit : null;
    }
}

/// <summary>The authenticated runtime identity.</summary>
/// <param name="Commit">The full build commit.</param>
/// <param name="TenantId">The installed tenant.</param>
/// <param name="NamespaceInstallationId">The installed namespace coverage identity.</param>
/// <param name="ReceiverTargetVersion">The independently looked-up current receiver target.</param>
/// <param name="StorageIdentity">The installed storage reference.</param>
/// <param name="AuthorityRevision">The current worker authority revision.</param>
public sealed record ConversationRuntimeIdentity(string Commit, string TenantId, string NamespaceInstallationId,
    string ReceiverTargetVersion, string StorageIdentity, string AuthorityRevision);
