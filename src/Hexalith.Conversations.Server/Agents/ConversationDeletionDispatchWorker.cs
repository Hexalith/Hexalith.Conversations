using Hexalith.EventStore.Client.Streams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Runs bounded ordered source delivery for one explicitly installed tenant namespace.</summary>
/// <remarks>The shared dispatcher retains its source checkpoint and stops at unavailable or poisoned entries.
/// A hosted retry never grants authority or treats submission as an accepted receipt.</remarks>
public sealed partial class ConversationDeletionDispatchWorker(
    IServiceScopeFactory scopes,
    ConversationTenantCatalogueRegistration registration,
    TimeProvider clock,
    ILogger<ConversationDeletionDispatchWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopes.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<SourcePublicationDispatcher>();
                SourcePublicationDispatchResult result = await dispatcher.DispatchAsync(
                    registration.Scope, maximumCount: 100, stoppingToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(result.FailureReason))
                {
                    DispatchBlocked(logger, result.FailureReason);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                DispatchFailed(logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), clock, stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 54101, Level = LogLevel.Warning,
        Message = "Conversation deletion dispatch retained the source checkpoint: {FailureReason}")]
    private static partial void DispatchBlocked(ILogger logger, string failureReason);

    [LoggerMessage(EventId = 54102, Level = LogLevel.Error,
        Message = "Conversation deletion dispatch could not complete the current pass")]
    private static partial void DispatchFailed(ILogger logger, Exception exception);
}
