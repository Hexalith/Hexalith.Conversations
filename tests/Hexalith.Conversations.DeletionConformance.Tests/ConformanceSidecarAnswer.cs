// <copyright file="ConformanceSidecarAnswer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Serializes the captured receiver answer through the sidecar response.</summary>
/// <param name="answer">The captured query answer.</param>
internal sealed class ConformanceSidecarAnswer(QueryResult answer) : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(answer, Web), Encoding.UTF8, "application/json"),
        });
}
