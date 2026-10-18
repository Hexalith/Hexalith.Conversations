using Hexalith.EventStore.Aspire;

// Repository-only test AppHost: composes the module through the platform-owned Aspire/Dapr helper.
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string modulePath = Path.GetFullPath(Path.Combine(
    builder.AppHostDirectory,
    "..", "..", "src", "Hexalith.Conversations.MinimalModule.Server", "Hexalith.Conversations.MinimalModule.Server.csproj"));
IResourceBuilder<ProjectResource> module = builder.AddProject("conversation-module", modulePath);
_ = module.AddAspireDaprDomainModule(new AspireDaprDomainModuleOptions("conversation-module", AspireDaprInfrastructureMode.Isolated));

builder.Build().Run();
