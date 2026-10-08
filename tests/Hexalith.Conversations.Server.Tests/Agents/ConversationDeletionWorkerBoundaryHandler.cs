namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>In-process HTTP boundary callback, preserving actual client serialization without claiming network authentication.</summary>
internal sealed class ConversationDeletionWorkerBoundaryHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => handler(request, cancellationToken);
}
