using System.Text.Json;
using Hexalith.Conversations.MinimalModule.Contracts;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.MinimalModule.Server;

/// <summary>
/// Module registration only: the domain assembly marker and the domain-specific dependency graph registered
/// beside the platform-hosted domain service. Subscription, projection/query runtime and publication stay
/// platform-owned; this module supplies thin domain handlers the platform discovers and dispatches.
/// </summary>
public static class ConversationModule
{
    /// <summary>The kebab-case domain served by this module.</summary>
    public const string Domain = "conversation";

    /// <summary>The query type served by <see cref="ConversationTitleQuery"/>.</summary>
    public const string TitleQueryType = "get-conversation-title";

    /// <summary>Registers the module on the platform-hosted domain service.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static WebApplicationBuilder AddMinimalConversationModule(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _ = builder.AddEventStoreDomainService(typeof(ConversationCreated).Assembly, typeof(ConversationModule).Assembly);
        _ = builder.Services.AddEventStoreDomainEvents(
            typeof(ConversationCreated).Assembly,
            options =>
            {
                EventStoreDomainEventsOptions domain = EventStoreDomainEventsOptions.ForDomain(Domain);
                options.TopicName = domain.TopicName;
                options.SubscriptionRoute = domain.SubscriptionRoute;
            });
        _ = builder.Services.AddSingleton<ConversationObservations>();
        _ = builder.Services.AddEventStoreDomainEventHandler<ConversationCreated, ConversationCreatedObserver>();
        return builder;
    }
}

/// <summary>Stateless full-replay projection of a conversation; the platform hosts the endpoint.</summary>
public sealed class ConversationProjection : IDomainProjectionHandler
{
    /// <inheritdoc/>
    public string Domain => ConversationModule.Domain;

    /// <inheritdoc/>
    public ProjectionResponse Project(ProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string title = string.Empty;
        foreach (ProjectionEventDto evt in request.Events ?? [])
        {
            if (evt.EventTypeName.EndsWith(nameof(ConversationCreated), StringComparison.Ordinal))
            {
                title = JsonSerializer.Deserialize<ConversationCreated>(evt.Payload)?.Title ?? title;
            }
        }

        return new ProjectionResponse(Domain, JsonSerializer.SerializeToElement(new { title }));
    }
}

/// <summary>Thin query handler returning the projected title; the platform hosts the query endpoint.</summary>
public sealed class ConversationTitleQuery : IDomainQueryHandler
{
    /// <inheritdoc/>
    public string Domain => ConversationModule.Domain;

    /// <inheritdoc/>
    public string QueryType => ConversationModule.TitleQueryType;

    /// <inheritdoc/>
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(QueryResult.FromPayload(JsonSerializer.SerializeToElement(new { query.AggregateId }), Domain));
    }
}

/// <summary>Records the conversations observed through the platform subscription.</summary>
public sealed class ConversationObservations
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _observed = new();

    /// <summary>Gets the aggregate identifiers observed so far.</summary>
    public IReadOnlyCollection<string> Observed => _observed;

    /// <summary>Records one observed conversation.</summary>
    /// <param name="conversationId">The conversation identifier.</param>
    public void Record(string conversationId) => _observed.Enqueue(conversationId);
}

/// <summary>Consumes published <see cref="ConversationCreated"/> events delivered by the platform subscription.</summary>
/// <param name="observations">The observation record.</param>
public sealed class ConversationCreatedObserver(ConversationObservations observations) : IEventStoreDomainEventHandler<ConversationCreated>
{
    /// <inheritdoc/>
    public Task HandleAsync(ConversationCreated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        observations.Record(@event.ConversationId);
        return Task.CompletedTask;
    }
}
