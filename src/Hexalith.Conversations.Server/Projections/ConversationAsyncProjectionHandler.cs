// <copyright file="ConversationAsyncProjectionHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Owns the production named Conversations read-model projection and its full-replay rebuild plan.
/// </summary>
/// <remarks>
/// Immediate dispatch intentionally uses the existing sequential policy writer: detail is durable before the
/// tenant index is attempted, and completion is returned only after both writes finish. A retry re-materializes
/// the same state and converges through the writer's idempotent replace/merge policies. Coordinated rebuild uses
/// the platform batch plan so both candidate keys are promoted by the EventStore coordinator.
/// </remarks>
public sealed class ConversationAsyncProjectionHandler : IAsyncDomainProjectionRebuildHandler
{
    /// <summary>The named production projection route.</summary>
    public const string ConversationReadModelProjectionType = "conversation-read-model";

    private static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(5);

    private readonly ConversationProjectionMaterializer _materializer;
    private readonly IReadModelStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationProjectionReadModelWriter _writer;

    /// <summary>Initializes the named projection handler.</summary>
    public ConversationAsyncProjectionHandler(
        ConversationProjectionMaterializer materializer,
        ConversationProjectionReadModelWriter writer,
        IReadModelStore store,
        TimeProvider? timeProvider = null)
    {
        _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public string Domain => ConversationProjectionHandler.ConversationDomain;

    /// <inheritdoc/>
    public string ProjectionType => ConversationReadModelProjectionType;

    /// <inheritdoc/>
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

    /// <inheritdoc/>
    public async Task<DomainProjectionHandlerResult> ProjectAsync(
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(dispatchId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Domain, Domain, StringComparison.Ordinal))
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.UnsupportedRoute);
        }

        ConversationProjectedReadModels models;
        try
        {
            models = Materialize(request);
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        if (models.Summary.Freshness.ReasonCode == ProjectionFreshnessReasonCode.PoisonEvent)
        {
            return DomainProjectionHandlerResult.Failed(ProjectionDispatchReasonCodes.HandlerFailure);
        }

        try
        {
            await _writer.PersistAsync(models, cancellationToken).ConfigureAwait(false);
            return DomainProjectionHandlerResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            return DomainProjectionHandlerResult.Retryable(ProjectionDispatchReasonCodes.PartialRetry);
        }
        catch (Exception)
        {
            return DomainProjectionHandlerResult.Indeterminate(ProjectionDispatchReasonCodes.HandlerFailure);
        }
    }

    /// <inheritdoc/>
    public async Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(
        ProjectionRequest request,
        string operationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(request.Domain, Domain, StringComparison.Ordinal))
        {
            throw new ArgumentException("The projection route is not supported.", nameof(request));
        }

        ConversationProjectedReadModels models = Materialize(request);
        if (models.Summary.Freshness.ReasonCode == ProjectionFreshnessReasonCode.PoisonEvent)
        {
            throw new ArgumentException("The projection input is outside the requested scope.", nameof(request));
        }

        ReadModelEntry<ConversationProjectedReadModels> persistedDetail = await _store
            .GetAsync<ConversationProjectedReadModels>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.ConversationKey(models.Summary.TenantId, models.Summary.ConversationId),
                cancellationToken)
            .ConfigureAwait(false);
        ReadModelEntry<ConversationProjectionIndexReadModel> persistedIndex = await _store
            .GetAsync<ConversationProjectionIndexReadModel>(
                ConversationProjectionReadModelKeys.StateStoreName,
                ConversationProjectionReadModelKeys.TenantIndexKey(models.Summary.TenantId),
                cancellationToken)
            .ConfigureAwait(false);
        ConversationProjectionIndexReadModel index = new()
        {
            Summaries = [.. (persistedIndex.Value?.Summaries ?? [])
                .Where(summary => summary.ConversationId != models.Summary.ConversationId)
                .Append(models.Summary)
                .OrderBy(summary => summary.ConversationId.Value, StringComparer.Ordinal)],
        };

        return new DomainProjectionRebuildPlan(
            ConversationProjectionReadModelKeys.StateStoreName,
            [
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.ConversationKey(models.Summary.TenantId, models.Summary.ConversationId),
                    models,
                    WriteConcurrency(persistedDetail)),
                ReadModelBatchOperation.Write(
                    ConversationProjectionReadModelKeys.TenantIndexKey(models.Summary.TenantId),
                    index,
                    WriteConcurrency(persistedIndex)),
            ]);
    }

    private ConversationProjectedReadModels Materialize(ProjectionRequest request)
        => _materializer.Project(
            new TenantId(request.TenantId),
            new ConversationId(request.AggregateId),
            ConversationProjectionEventDecoder.Decode(request.Events),
            _timeProvider.GetUtcNow(),
            DefaultStaleAfter,
            isRebuilding: false,
            metadataWriteFailed: false);

    private static ReadModelBatchConcurrency WriteConcurrency<TValue>(ReadModelEntry<TValue> entry)
        where TValue : class
        => string.IsNullOrEmpty(entry.ETag)
            ? ReadModelBatchConcurrency.CreateOnly
            : ReadModelBatchConcurrency.Match(entry.ETag);
}
