// <copyright file="ConversationEventStoreGatewayRegistration.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.TenantAccess;

/// <summary>
/// The single SDK EventStore gateway client registration that the enrolled tenant access and the opt-in deletion
/// runtime share: one typed client, its worker authority transport, and one gateway origin.
/// </summary>
/// <param name="BaseAddress">The registered gateway origin.</param>
internal sealed record ConversationEventStoreGatewayRegistration(Uri BaseAddress)
{
    /// <summary>The worker authority configuration section the gateway transport reads.</summary>
    internal const string AuthoritySection = "EventStore:Authentication";

    /// <summary>
    /// Validates a gateway origin.
    /// </summary>
    /// <param name="value">The configured origin.</param>
    /// <param name="gateway">The validated origin.</param>
    /// <returns>True for an HTTPS or loopback HTTP origin without credentials, query, fragment or path.</returns>
    internal static bool TryParse(string? value, out Uri gateway)
    {
        gateway = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/"
            || !(uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
        {
            return false;
        }

        gateway = uri;
        return true;
    }

    /// <summary>
    /// Lists the missing or unsupported worker authority keys, without their values.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The failing configuration keys.</returns>
    internal static IReadOnlyList<string> AuthorityFailures(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IConfigurationSection worker = configuration.GetSection(AuthoritySection);
        List<string> failures = [];
        foreach (string key in new[] { "Authority", "TokenEndpoint", "ClientId", "ClientSecret" })
        {
            if (string.IsNullOrWhiteSpace(worker[key]))
            {
                failures.Add(AuthoritySection + ":" + key);
            }
        }

        if (worker["GrantType"] is not (null or "client_credentials"))
        {
            failures.Add(AuthoritySection + ":GrantType");
        }

        return failures;
    }

    /// <summary>
    /// Registers the SDK gateway client once on the worker authority transport.
    /// </summary>
    /// <param name="services">The host registrations.</param>
    /// <param name="gateway">The validated gateway origin.</param>
    /// <param name="configuration">The host configuration holding the worker authority.</param>
    /// <param name="keepHostClient">
    /// Whether a gateway client the host registered itself (outside this registration) is kept instead of replaced.
    /// </param>
    /// <exception cref="InvalidOperationException">Another composition already registered a different gateway origin.</exception>
    internal static void Add(IServiceCollection services, Uri gateway, IConfiguration configuration, bool keepHostClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(configuration);
        ConversationEventStoreGatewayRegistration? existing = services
            .Select(static descriptor => descriptor.IsKeyedService ? null : descriptor.ImplementationInstance)
            .OfType<ConversationEventStoreGatewayRegistration>()
            .FirstOrDefault();
        if (existing is not null)
        {
            if (existing.BaseAddress != gateway)
            {
                throw new InvalidOperationException(
                    "Tenant access and the deletion runtime must use one EventStore gateway origin; the configured origins differ.");
            }

            return;
        }

        // The marker is recorded even when the host supplies its own client, so a later composition reuses that
        // client instead of registering a second one. Keyed clients never stand in for the unkeyed history reader.
        services.AddSingleton(new ConversationEventStoreGatewayRegistration(gateway));
        if (keepHostClient && services.Any(static descriptor => descriptor.ServiceType == typeof(IEventStoreGatewayClient)
            && !descriptor.IsKeyedService))
        {
            return;
        }

        services.AddEventStoreGatewayClient(options => options.BaseAddress = gateway)
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false })
            .AddEventStoreGatewayAuthorityTransport(configuration);
    }
}
