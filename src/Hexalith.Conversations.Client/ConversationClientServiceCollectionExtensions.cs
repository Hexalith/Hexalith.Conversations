// <copyright file="ConversationClientServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Registers the supported Conversations .NET client.
/// </summary>
public static class ConversationClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the supported typed Conversations v1 client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The endpoint configuration action.</param>
    /// <returns>The HTTP client builder.</returns>
    public static IHttpClientBuilder AddHexalithConversationsClient(
        this IServiceCollection services,
        Action<ConversationClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        ConversationClientOptions options = new();
        configure(options);
        Uri endpoint = ValidateEndpoint(options.Endpoint);

        return services.AddHttpClient<IConversationClient, ConversationClient>(client =>
        {
            client.BaseAddress = endpoint;
        });
    }

    private static Uri ValidateEndpoint(Uri? endpoint)
    {
        if (endpoint is null || !endpoint.IsAbsoluteUri)
        {
            throw new InvalidOperationException("Conversations client endpoint must be an absolute URI.");
        }

        return endpoint.Scheme is "http" or "https"
            ? endpoint
            : throw new InvalidOperationException("Conversations client endpoint must use http or https.");
    }
}
