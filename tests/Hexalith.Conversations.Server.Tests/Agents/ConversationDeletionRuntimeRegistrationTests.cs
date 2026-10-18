using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Proves the opt-in runtime cannot start with the normal unavailable authority defaults.</summary>
public sealed class ConversationDeletionRuntimeRegistrationTests
{
    /// <summary>Missing authenticated providers leave the runtime uninstalled.</summary>
    [Fact]
    public void UnavailableDefaultsCannotEnableDeletionRuntime()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddConversationAgentServices();
        var tenant = new TenantId("tenant-a");
        var catalogue = new ConversationTenantCatalogueRegistration(new SourcePublicationScope(
            tenant.Value, "conversation", "approved-deletion-v1", "installation-a"));
        var worker = new ConversationDeletionWorkerRegistration(tenant, "conversations-worker", new PartyId("service-party"));

        Should.Throw<InvalidOperationException>(() => services.AddConversationDeletionRuntime(catalogue, worker))
            .Message.ShouldContain("IConversationAgentAuthority");
    }
}
