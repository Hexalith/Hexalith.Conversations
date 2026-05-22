// <copyright file="ConversationQueryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Registers conversation query boundary services.
/// </summary>
public static class ConversationQueryServiceCollectionExtensions
{
    /// <summary>
    /// The configuration section that holds the cursor signing material and rotation identity.
    /// </summary>
    public const string CursorOptionsSectionName = "Hexalith:Conversations:Queries:Cursor";

    /// <summary>
    /// Adds conversation query handlers and their projection-read boundary.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root from which to bind cursor options.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// The host must bind <see cref="ConversationQueryCursorOptions.SigningKey"/> (at least 32 bytes of
    /// cryptographically random material, stored as base64 in configuration) and
    /// <see cref="ConversationQueryCursorOptions.KeyId"/> under the
    /// <c>Hexalith:Conversations:Queries:Cursor</c> section. Compile-time defaults are not provided
    /// because a hardcoded signing key cannot enforce the AC 7 "tampered cursors fail closed" requirement.
    /// </remarks>
    public static IServiceCollection AddConversationQueries(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ConversationQueryCursorOptions>(options =>
        {
            IConfigurationSection section = configuration.GetSection(CursorOptionsSectionName);
            string? base64Key = section["SigningKey"];
            if (!string.IsNullOrWhiteSpace(base64Key))
            {
                options.SigningKey = Convert.FromBase64String(base64Key);
            }

            string? keyId = section["KeyId"];
            if (!string.IsNullOrWhiteSpace(keyId))
            {
                options.KeyId = keyId;
            }

            string? maxAge = section["MaxAge"];
            if (!string.IsNullOrWhiteSpace(maxAge) && TimeSpan.TryParse(maxAge, out TimeSpan parsedMaxAge))
            {
                options.MaxAge = parsedMaxAge;
            }

            string? maxOffset = section["MaxOffset"];
            if (!string.IsNullOrWhiteSpace(maxOffset) && int.TryParse(maxOffset, out int parsedMaxOffset))
            {
                options.MaxOffset = parsedMaxOffset;
            }
        });

        return services.AddConversationQueriesCore();
    }

    /// <summary>
    /// Adds conversation query handlers using an explicit cursor options configuration callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCursor">Callback that supplies the cursor signing material and rotation identity.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConversationQueries(
        this IServiceCollection services,
        Action<ConversationQueryCursorOptions> configureCursor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureCursor);

        services.Configure(configureCursor);
        return services.AddConversationQueriesCore();
    }

    private static IServiceCollection AddConversationQueriesCore(this IServiceCollection services)
    {
        services.AddSingleton<ConversationQueryCursor>(static provider =>
            new ConversationQueryCursor(provider.GetRequiredService<IOptions<ConversationQueryCursorOptions>>()));
        services.TryAddSingleton<IConversationReferenceHydrationDirectory>(UnavailableConversationReferenceHydrationDirectory.Instance);
        services.AddScoped<ConversationReadHydrationService>();
        services.AddScoped<ConversationProjectionReadService>();
        services.AddScoped<ConversationAuditRecordAccessService>();
        services.AddScoped<ConversationQueryHandler>();
        return services;
    }
}
