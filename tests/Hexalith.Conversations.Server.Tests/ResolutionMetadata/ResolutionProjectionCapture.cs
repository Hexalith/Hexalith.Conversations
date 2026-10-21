using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionProjectionCapture : IAsyncDomainProjectionHandler
{
    public string Domain => Hexalith.Conversations.Server.Projections.ConversationProjectionHandler.ConversationDomain;
    public string ProjectionType => "native-fixture";
    public ProjectionRequest? Request { get; private set; }
    public Task<DomainProjectionHandlerResult> ProjectAsync(ProjectionRequest request, string dispatchId, CancellationToken cancellationToken)
    {
        Request = request;
        return Task.FromResult(DomainProjectionHandlerResult.Completed());
    }
}
