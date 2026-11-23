using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Exercises the real owner receiver when tenant write authority is temporarily unavailable.</summary>
public sealed class ConversationPilotWriteHostTest
{
    /// <summary>The authenticated SDK request sees the pre-processing marker before an aggregate can run.</summary>
    [Fact]
    public async Task TemporarilyUnavailableAuthorityReturnsThePrivateAdmissionMarker()
    {
        const string subject = "qualification-human";
        const string channel = "conversations-pilot-test-channel";
        const string issuer = "conversations-pilot-test-issuer";
        const string appId = ResolutionRouteCatalog.AppId;
        string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var access = Substitute.For<IConversationTenantAccessService>();
        access.CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, subject, commandTenantId: F.Tenant,
            cancellationToken: Arg.Any<CancellationToken>()).Returns(new ConversationTenantAccessDecision(false,
                ConversationTenantAccessRequirement.Write, F.Tenant, subject, ConversationTenantAccessDenialReason.TenantAccessUnavailable, true));

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            var settings = DeletionRuntimeTestSettings.Configuration(enabled: false);
            settings[DaprAppChannelToken.ConfigurationKey] = channel;
            settings["EventStore:DomainService:AppId"] = appId;
            settings["EventStore:DomainService:ServiceVersion"] = "v1";
            settings["Authentication:JwtBearer:Issuer"] = issuer;
            settings["Authentication:JwtBearer:Audience"] = "hexalith-eventstore";
            settings["Authentication:JwtBearer:SigningKey"] = signingKey;
            settings["Authentication:JwtBearer:AllowedAlgorithms:0"] = SecurityAlgorithms.HmacSha256;
            settings["Authentication:JwtBearer:RequireHttpsMetadata"] = "false";
            const string binding = "Conversations:Pilot:HumanPartyBindings:0:";
            settings[binding + "TenantId"] = F.Tenant.Value;
            settings[binding + "PrincipalId"] = subject;
            settings[binding + "PartyId"] = F.Human.Value;
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IConversationTenantAccessService>();
                services.AddSingleton(access);
                services.RemoveAll<IRouteCatalogAuthority>();
                services.AddSingleton<ResolutionRouteCatalog>(provider => new(provider, F.Tenant.Value,
                    () => provider.GetRequiredService<EventStoreDomainServiceEndpointSource>().Endpoints!.DataSources
                        .SelectMany(source => source.Endpoints), includePilotCreate: true));
                services.AddSingleton<IRouteCatalogAuthority>(provider => provider.GetRequiredService<ResolutionRouteCatalog>());
                string fingerprint = Convert.ToHexStringLower(SHA256.HashData("conversations-pilot-admission-test"u8));
                services.Configure<RouteCatalogHostOptions>(options => options.Identity = new(appId, appId, fingerprint, fingerprint, fingerprint));
            });
        });
        using var client = factory.CreateClient();
        var catalog = factory.Services.GetRequiredService<ResolutionRouteCatalog>().Snapshot;
        var fixture = new F();
        var metadata = fixture.CommandMetadata(F.Human);
        var id = new ConversationId(PilotCommandIdentity.Allocate(metadata, subject, "conversation"));
        var command = new CreateHumanConversation(new(metadata), id, F.At, PilotCommandIdentity.Allocate(metadata, subject, "created-event"));
        var envelope = fixture.Envelope(command, subject) with { AggregateId = id.Value };

        var now = DateTime.UtcNow;
        string assertion = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = appId,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(2),
            Claims = new Dictionary<string, object>
            {
                [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore",
                [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = new[] { EventStoreWorkloadOperations.DomainServiceProcess },
                [RouteCatalogSurfacePolicy.ClaimType] = "service",
            },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/process") { Content = JsonContent.Create(new DomainServiceRequest(envelope, null)) };
        request.Headers.Add(DaprAppChannelToken.HeaderName, channel);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, catalog.RootDigest);
        request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, catalog.Epoch.ToString(CultureInfo.InvariantCulture));

        var token = TestContext.Current.CancellationToken;
        using var response = await client.SendAsync(request, token);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.GetValues("X-Hexalith-Command-Admission").ShouldBe(["before-processing-v1"]);
        (await response.Content.ReadAsByteArrayAsync(token)).ShouldBeEmpty();
        await access.Received(1).CheckAccessAsync(ConversationTenantAccessRequirement.Write, F.Tenant, subject,
            commandTenantId: F.Tenant, cancellationToken: Arg.Any<CancellationToken>());
    }
}
