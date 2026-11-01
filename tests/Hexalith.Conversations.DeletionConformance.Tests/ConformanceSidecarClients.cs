// <copyright file="ConformanceSidecarClients.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Answers every sidecar query with one captured receiver answer.</summary>
/// <param name="answer">The answer returned to each query.</param>
internal sealed class ConformanceSidecarClients(QueryResult answer) : IHttpClientFactory
{
    /// <inheritdoc />
    public HttpClient CreateClient(string name) => new(new ConformanceSidecarAnswer(answer));
}
