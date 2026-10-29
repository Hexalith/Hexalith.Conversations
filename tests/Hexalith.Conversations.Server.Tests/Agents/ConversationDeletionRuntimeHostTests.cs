// <copyright file="ConversationDeletionRuntimeHostTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;
using S = Hexalith.Conversations.Server.Tests.Agents.DeletionRuntimeTestSettings;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Opt-in host composition with doubles for only the out-of-scope providers; local simulation, never live qualification.</summary>
public sealed class ConversationDeletionRuntimeHostTests
{
    private const string IdentityRoute = "/api/v1/ext-conv-ai-1/runtime-identity";
    private const string ParticipantRoute = "/api/v1/conversations/{conversationId}/participants";

    /// <summary>With the opt-in absent or false the host composes exactly as before and keeps its Unavailable defaults.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task DisabledConfigurationKeepsTheDefaultComposition(string? enabled)
    {
        Dictionary<string, string?> settings = S.Configuration(enabled: true);
        settings[ConversationDeletionRuntimeOptions.EnabledKey] = enabled;
        List<ServiceDescriptor> baselineDescriptors = [];
        List<ServiceDescriptor> hostDescriptors = [];
        await using WebApplication baseline = ComposeLikeProgram(settings, installed: null, optIn: false, descriptors: baselineDescriptors);
        await using WebApplication host = ComposeLikeProgram(settings, installed: null, optIn: true, descriptors: hostDescriptors);

        Describe(hostDescriptors).ShouldBe(Describe(baselineDescriptors));
        hostDescriptors.ShouldNotContain(static descriptor => descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(ConversationDeletionDispatchWorker));
        host.Services.GetService<ConversationDeletionRuntimeOptions>().ShouldBeNull();
        host.Services.GetService<IConversationClient>().ShouldBeNull();
        host.Services.GetRequiredService<IConversationCommandSourceVerifier>().ShouldBeOfType<UnavailableConversationCommandSourceVerifier>();
        host.Services.GetRequiredService<IConversationAgentAuthority>().ShouldBeOfType<UnavailableConversationAgentAuthority>();
        Routes(host).Keys.ShouldNotContain(IdentityRoute);
        Routes(host).Keys.ShouldNotContain(ParticipantRoute);
    }

    /// <summary>Each incomplete or unsafe value refuses before any provider check, naming its key without echoing the value.</summary>
    [Theory]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":TenantId", null, ConversationDeletionRuntimeOptions.SectionName + ":TenantId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupPrincipalId", S.Worker, ConversationDeletionRuntimeOptions.SectionName + ":DistinctPrincipals")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":GatewayBaseAddress", "http://gateway.example/", ConversationDeletionRuntimeOptions.SectionName + ":GatewayBaseAddress")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":AcceptedTargetVersions:1", "0123456789abcdef0123456789abcdef0123456", ConversationDeletionRuntimeOptions.SectionName + ":AcceptedTargetVersions")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":ServicePartyId", " ", ConversationDeletionRuntimeOptions.SectionName + ":ServicePartyId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":NamespaceInstallationId", "bad installation!", ConversationDeletionRuntimeOptions.SectionName + ":NamespaceInstallationId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":StorageIdentity", null, ConversationDeletionRuntimeOptions.SectionName + ":StorageIdentity")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":ReceiverPrincipalId", "receiver/app", ConversationDeletionRuntimeOptions.SectionName + ":ReceiverPrincipalId:AppId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":PlatformWorkloadId", "platform-workload-201", ConversationDeletionRuntimeOptions.SectionName + ":PlatformWorkloadId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:Workload", "other-lookup-workload", ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:Workload")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientId", "worker-client", ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientId")]
    [InlineData("Authentication:WorkloadIssuer:ClientId", "lookup-client", ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientId", null, ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientId")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientSecret", null, ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:ClientSecret")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:TokenEndpoint", null, ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:TokenEndpoint")]
    [InlineData(ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:EnableResourceBindingRequests", "false", ConversationDeletionRuntimeOptions.SectionName + ":LookupIssuer:EnableResourceBindingRequests")]
    [InlineData("EventStore:Authentication:Authority", null, "EventStore:Authentication:Authority")]
    [InlineData("EventStore:Authentication:TokenEndpoint", null, "EventStore:Authentication:TokenEndpoint")]
    [InlineData("EventStore:Authentication:ClientId", null, "EventStore:Authentication:ClientId")]
    [InlineData("EventStore:Authentication:ClientSecret", null, "EventStore:Authentication:ClientSecret")]
    [InlineData("EventStore:Authentication:GrantType", "password", "EventStore:Authentication:GrantType")]
    [InlineData("DAPR_API_TOKEN", null, "DAPR_API_TOKEN")]
    [InlineData("DAPR_HTTP_ENDPOINT", null, "DAPR_HTTP_ENDPOINT|DAPR_HTTP_PORT")]
    [InlineData("DAPR_HTTP_ENDPOINT", "http://sidecar.example:3500", "DAPR_HTTP_ENDPOINT|DAPR_HTTP_PORT")]
    public void IncompleteConfigurationRefusesWithoutEchoingValues(string key, string? value, string expectedKey)
    {
        Dictionary<string, string?> settings = S.Configuration();
        // InlineData holds a stand-in for a value longer than the SDK's 200-character platform workload bound.
        settings[key] = value == "platform-workload-201" ? new string('p', 201) : value;

        var refusal = Should.Throw<InvalidOperationException>(() => ComposeLikeProgram(settings, InstallAll(), optIn: true));

        refusal.Message.ShouldContain(expectedKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            refusal.Message.ShouldNotContain(settings[key]!);
        }

        refusal.Message.ShouldNotContain(S.WorkerSecret);
        refusal.Message.ShouldNotContain(S.LookupSecret);
        refusal.Message.ShouldNotContain(S.SidecarToken);
    }

    /// <summary>The in-scope pieces are supplied by the host; each missing out-of-scope provider still refuses startup, first first.</summary>
    [Fact]
    public void MissingInstalledProvidersRefuseNamingTheFirstMissing()
    {
        var authority = new StaticAuthority(new(ConversationAgentsOutcome.Denied));
        (string Missing, Action<IServiceCollection>? Installed)[] cases =
        [
            ("IConversationAgentAuthority", null),
            ("IConversationDeletionApprovalVerifier", services => services.AddSingleton<IConversationAgentAuthority>(authority)),
            ("ISourcePublicationNamespaceSource", services => services.AddSingleton<IConversationAgentAuthority>(authority)
                .AddSingleton<IConversationDeletionApprovalVerifier, DeniedApprovalVerifier>()),
            ("ISourcePublicationIndexStore", services => services.AddSingleton<IConversationAgentAuthority>(authority)
                .AddSingleton<IConversationDeletionApprovalVerifier, DeniedApprovalVerifier>()
                .AddSingleton<ISourcePublicationNamespaceSource, AbsentNamespaceSource>()),
        ];

        foreach ((string missing, Action<IServiceCollection>? installed) in cases)
        {
            Should.Throw<InvalidOperationException>(() => ComposeLikeProgram(S.Configuration(), installed, optIn: true))
                .Message.ShouldBe($"Authenticated Conversation runtime provider is missing: {missing}.");
        }
    }

    /// <summary>The enabled host registers the in-scope adapters, the SDK reader and complete-source admission, and maps authenticated routes.</summary>
    [Fact]
    public async Task EnabledHostComposesTheInScopeAdaptersAndAuthenticatedRoutes()
    {
        List<ServiceDescriptor> descriptors = [];
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(), optIn: true, descriptors: descriptors);
        using IServiceScope scope = host.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        services.GetRequiredService<IConversationCommandSourceVerifier>().ShouldBeOfType<CompleteSourceConversationCommandSourceVerifier>();
        services.GetRequiredService<IConversationDeletionReceiver>().ShouldBeOfType<ConformanceConversationDeletionReceiver>();
        services.GetRequiredService<IAuthoritativeEventStreamReader>().ShouldBeOfType<AuthoritativeEventStreamReader>();
        services.GetRequiredService<IConversationClient>().ShouldBeOfType<ConversationClient>();
        services.GetRequiredService<CompleteCommandSourceVerificationOptions>()
            .ShouldBe(new CompleteCommandSourceVerificationOptions(S.Platform, ["conversation"]), new VerificationOptionsComparer());
        services.GetRequiredService<ConversationDeletionWorkerRegistration>()
            .ShouldBe(new ConversationDeletionWorkerRegistration(F.Tenant, S.Worker, new PartyId(S.ServiceParty)));
        services.GetRequiredService<ConversationTenantCatalogueRegistration>().Scope.InstallationId.ShouldBe(S.Installation);
        descriptors.Count(static descriptor => descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(ConversationDeletionDispatchWorker)).ShouldBe(1);
        services.GetRequiredKeyedService<IWorkloadAssertionIssuer>(ConformanceConversationDeletionReceiver.LookupIssuerKey)
            .ShouldNotBeSameAs(services.GetRequiredService<IWorkloadAssertionIssuer>(), "lookups never borrow the host workload issuer");

        IReadOnlyDictionary<string, RouteEndpoint> routes = Routes(host);
        foreach (string route in new[] { IdentityRoute, ParticipantRoute })
        {
            routes.Keys.ShouldContain(route);
            routes[route].Metadata.GetOrderedMetadata<IAuthorizeData>().Select(static data => data.Policy)
                .ShouldContain(ConversationDeletionRuntimeHostExtensions.CallerPolicy);
        }
    }

    /// <summary>Sidecar lookups, gateway submissions and Conversations client calls all use redirect-free primary handlers.</summary>
    [Fact]
    public async Task OutboundClientsDisableRedirects()
    {
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(), optIn: true);
        var handlers = host.Services.GetRequiredService<IHttpMessageHandlerFactory>();
        foreach (string name in new[] { ConformanceConversationDeletionReceiver.LookupClientName, "IConversationClient", "IEventStoreGatewayClient" })
        {
            HttpMessageHandler handler = handlers.CreateHandler(name);
            while (handler is DelegatingHandler delegating)
            {
                handler = delegating.InnerHandler!;
            }

            handler.ShouldBeOfType<SocketsHttpHandler>(name).AllowAutoRedirect.ShouldBeFalse(name);
        }

        JwtBearerOptions caller = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(ConversationDeletionRuntimeHostExtensions.CallerScheme);
        caller.BackchannelHttpHandler.ShouldBeOfType<SocketsHttpHandler>().AllowAutoRedirect.ShouldBeFalse();
    }

    /// <summary>The composed SDK reader requests native proof and treats a proof-less page as non-authoritative.</summary>
    [Fact]
    public async Task ComposedStreamReaderRequiresNativeProof()
    {
        var transport = new DeletionRuntimeTransport();
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(), optIn: true, transport);
        using IServiceScope scope = host.Services.CreateScope();

        AuthoritativeStreamReadResult result = await scope.ServiceProvider.GetRequiredService<IAuthoritativeEventStreamReader>()
            .ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken);

        result.IsAuthoritative.ShouldBeFalse();
        result.FailureReason.ShouldBe("source-native-proof-unavailable");
        transport.StreamReads.ShouldNotBeEmpty();
        transport.StreamReads.ShouldAllBe(static request => request.RequireNativeProof);
        transport.GatewayRequests.ShouldAllBe(static request => request.Authorization == "Bearer " + S.WorkerToken);
    }

    /// <summary>The real keyed lookup issuer binds resources and signs as the lookup workload, never the host workload.</summary>
    [Fact]
    public async Task RealLookupIssuerSignsAsTheLookupWorkload()
    {
        Dictionary<string, string?> settings = S.SymmetricConfiguration();
        settings["EventStore:DomainService:AppId"] = "conversations";
        await using WebApplication host = ComposeLikeProgram(settings, InstallAll(), optIn: true, environment: Environments.Development);
        var lookup = host.Services.GetRequiredKeyedService<IWorkloadAssertionIssuer>(ConformanceConversationDeletionReceiver.LookupIssuerKey);
        var bindings = new Dictionary<string, string>
        {
            [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = F.Tenant.Value,
            [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = ConversationDeletionRuntimeOptions.ReceiverDomain,
        };

        lookup.CanBindResources.ShouldBeTrue();
        string? assertion = await lookup.IssueAsync(new WorkloadAssertionRequest(S.Receiver, EventStoreWorkloadOperations.DomainServiceQuery,
            bindings), TestContext.Current.CancellationToken);

        var token = new JsonWebToken(assertion.ShouldNotBeNull());
        token.Claims.Where(static claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.CallerClaimType).Select(static claim => claim.Value)
            .ShouldBe([S.Lookup]);
        token.Audiences.ShouldBe([S.Receiver]);
        JwtWorkloadAssertionIssuer.GetTokenScopeFailure(assertion, S.Receiver, EventStoreWorkloadOperations.DomainServiceQuery).ShouldBeNull();
        foreach (KeyValuePair<string, string> binding in bindings)
        {
            token.Claims.Single(claim => claim.Type == binding.Key).Value.ShouldBe(binding.Value);
        }

        string? hostAssertion = await host.Services.GetRequiredService<IWorkloadAssertionIssuer>().IssueAsync(
            new WorkloadAssertionRequest(S.Receiver, EventStoreWorkloadOperations.DomainServiceQuery), TestContext.Current.CancellationToken);
        new JsonWebToken(hostAssertion.ShouldNotBeNull()).Claims.Single(static claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.CallerClaimType)
            .Value.ShouldBe("conversations", "the host issuer keeps its own workload");
    }

    /// <summary>The pump delivers through the receiver adapter and advances only after an independently looked-up exact acknowledgement.</summary>
    [Fact]
    public async Task PumpAdvancesOnlyAfterAnIndependentExactAcknowledgement()
    {
        ConversationDeletionDeliveryPumpFixture source = await S.ApprovedSourceAsync();
        var transport = new DeletionRuntimeTransport(source);
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(source), optIn: true, transport);
        CancellationToken token = TestContext.Current.CancellationToken;

        async Task<ConversationAgentsOutcome> DeliverAsync()
        {
            using IServiceScope scope = host.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ConversationDeletionDeliveryPump>().DeliverAsync(source.Entry, token);
        }

        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Unavailable, "gateway admission is pending, never a receipt");
        ConversationDeletionSourceResult pending = (await source.Source.ReplayAsync()).DeletionSource;
        pending.LastAttemptId.ShouldNotBeNull();
        pending.TargetVersion.ShouldBe(S.Target);
        pending.AcknowledgedSourceRevision.ShouldBe(0);
        ToolConformanceDeletion submitted = transport.Submissions.ShouldHaveSingleItem();
        submitted.ShouldBe(new ToolConformanceDeletion(pending.Signal!, pending.LastAttemptId, S.Target));

        transport.ReceiverQueryFails = true;
        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Unavailable);
        transport.ReceiverQueryFails = false;
        transport.MalformedReceipt = true;
        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Unavailable, "a malformed receipt is not an acknowledgement");
        (await source.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
        (await source.Source.ReplayAsync()).DeletionSource.PoisonCode.ShouldBeNull("a malformed answer is transient, not poison");

        transport.MalformedReceipt = false;
        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Available);
        ConversationDeletionSourceResult committed = (await source.Source.ReplayAsync()).DeletionSource;
        committed.AcknowledgedSourceRevision.ShouldBe(source.Entry.Publication.SourceRevision);
        committed.Acknowledgement.ShouldBe(transport.Accepted(submitted.Signal.ConversationDeletionSignalId));
        transport.Submissions.Count.ShouldBe(1, "the lost acknowledgement is recovered by lookup, not by a second dispatch");
        source.AttemptCommands.Count.ShouldBe(1);

        transport.GatewayRequests.ShouldNotBeEmpty();
        transport.GatewayRequests.ShouldAllBe(request => request.Authorization == "Bearer " + S.WorkerToken
            && request.SidecarToken == null && request.Assertion == null);
        transport.SidecarRequests.ShouldNotBeEmpty();
        transport.SidecarRequests.ShouldAllBe(request => request.Authorization == null && request.SidecarToken == S.SidecarToken
            && !string.IsNullOrWhiteSpace(request.Assertion));
    }

    /// <summary>A well-formed authenticated receipt with changed same-id fields quarantines the entry without acknowledging it.</summary>
    [Fact]
    public async Task MismatchedReceiptQuarantinesWithoutAcknowledgement()
    {
        ConversationDeletionDeliveryPumpFixture source = await S.ApprovedSourceAsync();
        var transport = new DeletionRuntimeTransport(source);
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(source), optIn: true, transport);
        CancellationToken token = TestContext.Current.CancellationToken;

        async Task<ConversationAgentsOutcome> DeliverAsync()
        {
            using IServiceScope scope = host.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ConversationDeletionDeliveryPump>().DeliverAsync(source.Entry, token);
        }

        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Unavailable);
        transport.MismatchedReceipt = true;

        (await DeliverAsync()).ShouldBe(ConversationAgentsOutcome.Quarantined);

        ConversationDeletionSourceResult quarantined = (await source.Source.ReplayAsync()).DeletionSource;
        quarantined.Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined);
        quarantined.PoisonCode.ShouldNotBeNullOrWhiteSpace();
        quarantined.Acknowledgement.ShouldBeNull();
        quarantined.AcknowledgedSourceRevision.ShouldBe(0);
        transport.Submissions.Count.ShouldBe(1);
    }

    /// <summary>Through the real pipeline, the participant route identifies the caller as the validated token subject only.</summary>
    [Fact]
    public async Task ParticipantRouteMapsTheTokenSubjectAndRejectsAPresetNameIdentifier()
    {
        var authority = new StaticAuthority(new(ConversationAgentsOutcome.Denied));
        await using WebApplication host = ComposeLikeProgram(S.SymmetricConfiguration(), InstallAll(authority), optIn: true,
            environment: Environments.Development);
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();
        AddParticipantCommand command = new F().Membership.PublicCommand;
        string body = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        async Task<HttpResponseMessage> PostAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/conversations/{command.ConversationId.Value}/participants")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new("Bearer", token);
            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        using HttpResponseMessage denied = await PostAsync(S.CallerToken("agents-service",
            claims: new Dictionary<string, object> { ["tid"] = F.Tenant.Value }));
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        authority.Calls.ShouldHaveSingleItem().Principal.ShouldBe("agents-service");

        using HttpResponseMessage preset = await PostAsync(S.CallerToken("agents-service", claims: new Dictionary<string, object>
        {
            ["tid"] = F.Tenant.Value,
            [ClaimTypes.NameIdentifier] = "another-principal",
        }));
        preset.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        authority.Calls.Count.ShouldBe(1, "an ambiguous caller never reaches authority");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>A request forwards its own Bearer, a request without one fails unsent, and only background calls use the worker token.</summary>
    [Fact]
    public async Task ConversationClientNeverMixesCallerAndWorkerIdentities()
    {
        ConversationDeletionDeliveryPumpFixture source = await S.ApprovedSourceAsync();
        var transport = new DeletionRuntimeTransport(source);
        await using WebApplication host = ComposeLikeProgram(S.Configuration(), InstallAll(source), optIn: true, transport);
        var accessor = host.Services.GetRequiredService<IHttpContextAccessor>();
        var query = new ConversationDeletionSourceQuery(F.Tenant, F.Conversation);
        CancellationToken token = TestContext.Current.CancellationToken;

        async Task<ConversationDeletionSourceResult> ReadAsync()
        {
            using IServiceScope scope = host.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IConversationClient>().GetConversationDeletionSourceAsync(query, token);
        }

        var caller = new DefaultHttpContext();
        caller.Request.Headers.Authorization = "Bearer caller-access-token";
        accessor.HttpContext = caller;
        (await ReadAsync()).Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        transport.GatewayRequests.Last().Authorization.ShouldBe("Bearer caller-access-token");

        int sent = transport.Handler.Requests.Count;
        accessor.HttpContext = new DefaultHttpContext();
        (await ReadAsync()).Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
        transport.Handler.Requests.Count.ShouldBe(sent, "a caller without a Bearer never falls back to the worker");

        accessor.HttpContext = null;
        (await ReadAsync()).Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        transport.GatewayRequests.Last().Authorization.ShouldBe("Bearer " + S.WorkerToken);
    }

    /// <summary>The configured reader receives the exact installed identity; any other caller is forbidden.</summary>
    [Fact]
    public async Task RuntimeIdentityIsReportedOnlyToTheConfiguredReader()
    {
        var authority = new StaticAuthority(Authorized());
        CancellationToken token = TestContext.Current.CancellationToken;

        IResult result = await ConversationRuntimeIdentityEndpoint.HandleAsync(Caller(S.Reader), S.Options(), authority,
            new StaticReceiver(S.Target), S.RolloverTarget, token);

        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(StatusCodes.Status200OK);
        ((IValueHttpResult)result).Value.ShouldBe(new ConversationRuntimeIdentity(S.RolloverTarget, F.Tenant.Value, S.Installation, S.Target,
            S.Storage, "authority-revision-7"));
        JsonSerializer.SerializeToElement(((IValueHttpResult)result).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .EnumerateObject().Select(static property => property.Name)
            .ShouldBe(["commit", "tenantId", "namespaceInstallationId", "receiverTargetVersion", "storageIdentity", "authorityRevision"]);
        authority.Calls.ShouldHaveSingleItem().ShouldBe((S.Worker, F.Tenant, (ConversationId?)null, "DeletionSource"));

        foreach (ClaimsPrincipal other in new[] { Caller(S.Worker), Caller(S.Lookup), new ClaimsPrincipal(new ClaimsIdentity()),
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", S.Reader), new Claim("sub", S.Worker)], "Bearer")) })
        {
            ((IStatusCodeHttpResult)await ConversationRuntimeIdentityEndpoint.HandleAsync(other, S.Options(), authority,
                new StaticReceiver(S.Target), S.RolloverTarget, token)).StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>Through the real pipeline, only a validated shared-contract Bearer whose subject is the configured reader reads the identity.</summary>
    [Fact]
    public async Task RuntimeIdentityRouteAuthenticatesCallersWithTheSharedContract()
    {
        var transport = new DeletionRuntimeTransport();
        await using WebApplication host = ComposeLikeProgram(S.SymmetricConfiguration(), InstallAll(new StaticAuthority(Authorized())),
            optIn: true, transport, environment: Environments.Development);
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = host.GetTestClient();

        async Task<HttpResponseMessage> GetAsync(string? token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, IdentityRoute);
            if (token is not null)
            {
                request.Headers.Authorization = new("Bearer", token);
            }

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        (await GetAsync(null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await GetAsync(S.CallerToken(S.Reader, issuer: "https://other.test/"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await GetAsync(S.CallerToken(S.Worker))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using HttpResponseMessage reader = await GetAsync(S.CallerToken(S.Reader));
        string? commit = ConversationRuntimeIdentityEndpoint.CommitOf(typeof(ServerAssemblyMarker).Assembly);
        if (commit is null)
        {
            reader.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable, "a build without its commit cannot report an identity");
        }
        else
        {
            reader.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await reader.Content.ReadFromJsonAsync<ConversationRuntimeIdentity>(TestContext.Current.CancellationToken))
                .ShouldBe(new ConversationRuntimeIdentity(commit, F.Tenant.Value, S.Installation, S.Target, S.Storage, "authority-revision-7"));
        }

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>A missing commit, receiver target or authority revision is unavailable rather than inferred.</summary>
    [Theory]
    [InlineData("commit")]
    [InlineData("receiver-target")]
    [InlineData("unaccepted-target")]
    [InlineData("authority-revision")]
    [InlineData("denied-authority")]
    [InlineData("other-party")]
    [InlineData("authority-failure")]
    public async Task MissingRuntimeIdentityFactsAreUnavailable(string missing)
    {
        ConversationAgentAuthorization authorized = Authorized();
        var authority = new StaticAuthority(missing switch
        {
            "authority-revision" => authorized with { AuthorityRevision = null },
            "denied-authority" => new ConversationAgentAuthorization(ConversationAgentsOutcome.Denied),
            "other-party" => authorized with { PartyId = F.Agent },
            _ => authorized,
        })
        {
            Failure = missing == "authority-failure" ? new IOException("authority unavailable") : null,
        };
        string? target = missing switch { "receiver-target" => null, "unaccepted-target" => new string('a', 40), _ => S.Target };

        IResult result = await ConversationRuntimeIdentityEndpoint.HandleAsync(Caller(S.Reader), S.Options(), authority,
            new StaticReceiver(target), missing == "commit" ? null : S.RolloverTarget, TestContext.Current.CancellationToken);

        ((IStatusCodeHttpResult)result).StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable, missing);
    }

    /// <summary>The commit is the full lowercase build suffix of the informational version.</summary>
    [Theory]
    [InlineData("1.0.0+" + S.Target, S.Target)]
    [InlineData("1.0.0-alpha.1+" + S.RolloverTarget, S.RolloverTarget)]
    [InlineData("1.0.0", null)]
    [InlineData("1.0.0+0123456", null)]
    [InlineData("1.0.0+0123456789ABCDEF0123456789ABCDEF01234567", null)]
    [InlineData(null, null)]
    public void CommitComesFromTheInformationalVersionSuffix(string? version, string? expected)
        => ConversationRuntimeIdentityEndpoint.ParseCommit(version).ShouldBe(expected);

    internal static WebApplication ComposeLikeProgram(Dictionary<string, string?> settings, Action<IServiceCollection>? installed, bool optIn,
        DeletionRuntimeTransport? transport = null, List<ServiceDescriptor>? descriptors = null, string? environment = null)
    {
        WebApplicationBuilder builder = environment is null ? WebApplication.CreateBuilder()
            : WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        if (environment is not null)
        {
            builder.WebHost.UseTestServer();
        }

        builder.Configuration.AddInMemoryCollection(settings);

        // The Program.cs sequence; installed out-of-scope providers are registered by the installation before the opt-in.
        builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationTenantAccess(builder.Configuration);
        builder.Services.AddConversationQueries(builder.Configuration);
        installed?.Invoke(builder.Services);
        bool enabled = optIn && builder.Configuration.GetValue<bool>(ConversationDeletionRuntimeOptions.EnabledKey);
        if (enabled)
        {
            builder.Services.AddConversationDeletionRuntimeHost(builder.Configuration);
        }

        if (transport is not null)
        {
            builder.Services.AddKeyedSingleton<IWorkloadAssertionIssuer>(ConformanceConversationDeletionReceiver.LookupIssuerKey, new FakeLookupIssuer());
            builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new PrimaryHandlerOverride(transport.Handler));
        }

        descriptors?.AddRange(builder.Services);
        WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        if (enabled)
        {
            app.MapConversationDeletionRuntimeHost();
        }

        return app;
    }

    private static Action<IServiceCollection> InstallAll(IConversationAgentAuthority? authority = null)
        => services => services.AddSingleton(authority ?? new StaticAuthority(new(ConversationAgentsOutcome.Denied)))
            .AddSingleton<IConversationDeletionApprovalVerifier, DeniedApprovalVerifier>()
            .AddSingleton<ISourcePublicationNamespaceSource, AbsentNamespaceSource>()
            .AddSingleton<ISourcePublicationIndexStore, AbsentIndexStore>();

    private static ConversationAgentAuthorization Authorized()
        => new(ConversationAgentsOutcome.Available, F.Tenant, S.Worker, new PartyId(S.ServiceParty), "authority-revision-7", true);

    private static ClaimsPrincipal Caller(string subject) => new(new ClaimsIdentity([new Claim("sub", subject)], "Bearer"));

    private static string[] Describe(IEnumerable<ServiceDescriptor> descriptors)
        => [.. descriptors.Select(static descriptor => string.Join("|",
            descriptor.ServiceType.FullName, descriptor.Lifetime, descriptor.IsKeyedService ? descriptor.ServiceKey : null,
            descriptor.IsKeyedService ? descriptor.KeyedImplementationType?.FullName : descriptor.ImplementationType?.FullName))];

    private static IReadOnlyDictionary<string, RouteEndpoint> Routes(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints).OfType<RouteEndpoint>()
            .GroupBy(static endpoint => "/" + endpoint.RoutePattern.RawText!.TrimStart('/'))
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

    private sealed class VerificationOptionsComparer : IEqualityComparer<CompleteCommandSourceVerificationOptions>
    {
        public bool Equals(CompleteCommandSourceVerificationOptions? x, CompleteCommandSourceVerificationOptions? y)
            => x is not null && y is not null && x.PlatformWorkloadId == y.PlatformWorkloadId && x.RequiredDomains.SequenceEqual(y.RequiredDomains);

        public int GetHashCode(CompleteCommandSourceVerificationOptions obj) => obj.PlatformWorkloadId.GetHashCode(StringComparison.Ordinal);
    }
}
