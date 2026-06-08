// <copyright file="ConversationClientServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.Http;

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
    /// <remarks>
    /// Thin facade over the shared <see cref="HttpClientRegistration.AddTypedHttpClient{TClient, TImplementation, TOptions}(IServiceCollection, Action{TOptions}, Func{TOptions, Uri}, HttpClientEndpointValidation, bool)"/>
    /// helper. The hand-rolled endpoint validation and registration logic has been removed (FR-17); the
    /// shared helper preserves the Conversations behavior exactly — eager (registration-time) validation
    /// that rejects a missing, relative, or non-http(s) endpoint.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The endpoint configuration action.</param>
    /// <returns>The HTTP client builder.</returns>
    public static IHttpClientBuilder AddHexalithConversationsClient(
        this IServiceCollection services,
        Action<ConversationClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddTypedHttpClient<IConversationClient, ConversationClient, ConversationClientOptions>(
            configure,
            static options => options.Endpoint,
            HttpClientEndpointValidation.OnRegistration,
            requireWebScheme: true);
    }
}
