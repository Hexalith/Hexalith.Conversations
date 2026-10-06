// <copyright file="NonCooperativeAgentHttpFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Client.Tests;

/// <summary>Holds a local HTTP send open without honoring caller cancellation.</summary>
internal sealed class NonCooperativeAgentHttpFixture : HttpMessageHandler
{
    private readonly TaskCompletionSource<HttpResponseMessage> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _sendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the signal that the noncooperative HTTP send has started.</summary>
    internal Task SendStarted => _sendStarted.Task;

    /// <summary>Completes the abandoned send with an owned response.</summary>
    /// <param name="response">The eventual response whose disposal is checked.</param>
    internal void Complete(HttpResponseMessage response) => _response.SetResult(response);

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sendStarted.SetResult();
        return _response.Task;
    }
}
