// <copyright file="ConversationsServiceDefaults.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Hexalith.Conversations.ServiceDefaults;

/// <summary>
/// Conversations-owned hooks over the shared ServiceDefaults base.
/// </summary>
public static class ConversationsServiceDefaults
{
    /// <summary>
    /// Gets the Conversations service and metric source name.
    /// </summary>
    public const string ServiceName = "Hexalith.Conversations";

    /// <summary>
    /// Adds Conversations service defaults without introducing a second runtime registration path.
    /// </summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder AddConversationsServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
        => builder.AddHexalithServiceDefaults(ConfigureConversationsDefaults);

    /// <summary>
    /// Configures the module-specific Conversations names on the shared base.
    /// </summary>
    /// <param name="options">The shared options.</param>
    public static void ConfigureConversationsDefaults(HexalithServiceDefaultsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ServiceName = ServiceName;
        options.MeterNames.Add(ServiceName);
    }
}
