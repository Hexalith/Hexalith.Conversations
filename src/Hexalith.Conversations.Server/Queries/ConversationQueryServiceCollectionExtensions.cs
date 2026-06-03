// <copyright file="ConversationQueryServiceCollectionExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Hydration;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Registration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Registers conversation query boundary services.
/// </summary>
public static class ConversationQueryServiceCollectionExtensions
{
    /// <summary>
    /// The stable, domain-unique ASP.NET Core Data Protection purpose isolating conversation list cursors from
    /// every other domain's cursors. Changing it invalidates outstanding cursors, which is a safe failure.
    /// </summary>
    public const string CursorCodecPurpose = "Hexalith.Conversations.QueryCursor.v1";

    /// <summary>
    /// The configuration section that holds the cursor domain-policy bounds (max age, max offset).
    /// </summary>
    public const string CursorOptionsSectionName = "Hexalith:Conversations:Queries:Cursor";

    /// <summary>
    /// Adds conversation query handlers and their projection-read boundary.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root from which to bind cursor policy bounds.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// Cursor integrity is owned by the platform <see cref="Hexalith.EventStore.Client.Queries.IQueryCursorCodec"/>
    /// (ASP.NET Core Data Protection), so no signing key or key id is bound here — only the optional
    /// <see cref="ConversationQueryCursorOptions.MaxAge"/> / <see cref="ConversationQueryCursorOptions.MaxOffset"/>
    /// domain-policy bounds the handler re-applies after a successful decode.
    /// </remarks>
    public static IServiceCollection AddConversationQueries(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ConversationQueryCursorOptions>(options =>
        {
            IConfigurationSection section = configuration.GetSection(CursorOptionsSectionName);

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
    /// <param name="configureCursor">Callback that supplies the cursor domain-policy bounds.</param>
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
        // Platform protected-cursor codec (Data Protection backed). TryAddSingleton-keyed by the codec so a
        // host that already registered it (or a test composition) is not overwritten.
        services.AddEventStoreQueryCursorCodec(CursorCodecPurpose);

        // Persisted read-model substrate (FR-5). The SDK store (TryAddSingleton<IReadModelStore,
        // DaprReadModelStore>) backs both the production read-store binding and the write-via-policy seam; it
        // resolves a DaprClient (registered in Program.cs). TryAdd* leaves a test composition free to override
        // IReadModelStore or IConversationProjectionReadStore with a fake.
        services.AddEventStoreReadModelStore();
        services.TryAddSingleton<IConversationProjectionReadStore, ConversationProjectionReadStore>();
        services.TryAddSingleton<ConversationProjectionReadModelWriter>();

        // The conversation-specific materialization logic shared by the read/rebuild path and the platform
        // projection seam handler (ConversationProjectionHandler, FR-6). Stateless, so a singleton; TryAdd keeps
        // it idempotent with AddConversationGovernanceVerification, which also registers it.
        services.TryAddSingleton<ConversationProjectionMaterializer>();

        services.TryAddSingleton<IConversationReferenceHydrationDirectory>(UnavailableConversationReferenceHydrationDirectory.Instance);
        services.AddScoped<ConversationReadHydrationService>();
        services.AddScoped<ConversationProjectionReadService>();
        services.AddScoped<ConversationAuditRecordAccessService>();
        services.TryAddSingleton<IPrivilegedOperationalJustificationReviewSource>(
            UnavailablePrivilegedOperationalJustificationReviewSource.Instance);
        services.AddScoped<ConversationPrivilegedJustificationReviewService>();
        services.AddScoped<ConversationQueryHandler>();
        return services;
    }
}
