// <copyright file="CallerMetadataPublicationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Api;
using Hexalith.Conversations.Server.Publication;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Server.Tests.Publication;

/// <summary>
/// Verifies caller-supplied provenance metadata stays provenance only (AC2) and never leaks unsafe values into
/// published transport metadata (AC3). Caller metadata never becomes tenant truth, authorization, or trust state.
/// </summary>
public sealed class CallerMetadataPublicationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly TenantId OtherTenant = new("tenant-002");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensures the safe publication header surface only carries Conversations-owned bounded identifiers and never
    /// any caller-supplied client/composer/origin value. Caller provenance flows through correlation/causation on the
    /// existing event metadata envelope, not as a new transport-leaking field.
    /// </summary>
    [Fact]
    public void TransportMetadataShouldNotCarryCallerSuppliedProvenanceValues()
    {
        ConversationCreated e = new(
            PublicationSamples.CreatedMetadata,
            PublicationSamples.Business,
            PublicationSamples.Project,
            PublicationSamples.Folder,
            "Case 123",
            PublicationSamples.ProviderCorrelation);

        ConversationTransportMetadata metadata = ConversationTransportMetadata.FromEvent(e);

        string combined = string.Join(
            '|',
            [metadata.Topic, metadata.Type, metadata.Source, metadata.Subject, .. metadata.Headers.Keys, .. metadata.Headers.Values]);

        // Correlation/causation (the canonical provenance carrier) are present and safe.
        metadata.Headers["correlationId"].ShouldBe("correlation-001");
        metadata.Headers["causationId"].ShouldBe("causation-001");

        // No caller-supplied client/composer/origin value is published as transport metadata.
        combined.ShouldNotContain("adopter-client", Case.Insensitive);
        combined.ShouldNotContain("front-composer", Case.Insensitive);
        combined.ShouldNotContain("adopter-portal", Case.Insensitive);
        combined.ShouldNotContain("callerMetadata", Case.Insensitive);
    }

    /// <summary>
    /// Ensures caller metadata claiming a different tenant or an elevated origin does NOT alter the trusted tenant
    /// binding decided from claims; the command still binds to the authenticated tenant context.
    /// </summary>
    [Fact]
    public async Task CallerMetadataClaimingOtherTenantShouldNotOverrideClaimsDerivedTenant()
    {
        FakeCommandHandler handler = new()
        {
            CreateOutcome = ConversationCommandApiOutcome<ConversationCreatedResult>.Success(
                CreatedResult(),
                StatusCodes.Status201Created),
        };
        using WebApplication app = BuildApp(handler);

        // Caller metadata advertises a different tenant and an "elevated" origin as provenance text only.
        CallerMetadata spoofingCaller = new(
            SchemaVersion.Current,
            "adopter-client",
            Origin: "adopter-portal",
            IntegrationContext: "elevated-origin");

        CreateConversationCommand command = new(
            new ConversationCommandMetadata(SchemaVersion.Current, Tenant, Actor, "corr-001", "cause-001", "idem-001"),
            Label: "Case 123",
            CallerMetadata: spoofingCaller);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/", command, AuthenticatedUser(Tenant));

        response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        handler.LastCreateCommand.ShouldNotBeNull();

        // Trusted tenant binding stays the claims-derived tenant; caller-supplied provenance never overrides it.
        handler.LastCreateCommand!.Metadata.TenantId.ShouldBe(Tenant);
        handler.LastCreateCommand.Metadata.TenantId.ShouldNotBe(OtherTenant);
    }

    /// <summary>
    /// Ensures valid caller-supplied provenance survives the command boundary intact so attribution remains useful
    /// (AC4), while the tenant binding still derives from the authenticated claims and never from caller metadata.
    /// </summary>
    [Fact]
    public async Task ValidCallerMetadataShouldFlowThroughIntactAsProvenance()
    {
        FakeCommandHandler handler = new()
        {
            CreateOutcome = ConversationCommandApiOutcome<ConversationCreatedResult>.Success(
                CreatedResult(),
                StatusCodes.Status201Created),
        };
        using WebApplication app = BuildApp(handler);

        CallerMetadata caller = new(
            SchemaVersion.Current,
            "adopter-client",
            "1.4.0",
            "front-composer",
            "adopter-portal",
            "intake",
            new Dictionary<string, string> { ["channel"] = "web" });

        CreateConversationCommand command = new(
            new ConversationCommandMetadata(SchemaVersion.Current, Tenant, Actor, "corr-001", "cause-001", "idem-001"),
            Label: "Case 123",
            CallerMetadata: caller);

        ApiResponse response = await InvokeAsync(app, "/api/v1/conversations/", command, AuthenticatedUser(Tenant));

        response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        handler.LastCreateCommand.ShouldNotBeNull();

        // Provenance is preserved as supplied (attribution usefulness)...
        CallerMetadata? received = handler.LastCreateCommand!.CallerMetadata;
        received.ShouldNotBeNull();
        received!.ClientName.ShouldBe("adopter-client");
        received.ComposerSource.ShouldBe("front-composer");
        received.Origin.ShouldBe("adopter-portal");
        received.ExtensionData!["channel"].ShouldBe("web");

        // ...but the tenant binding remains the claims-derived tenant, never a caller-supplied value.
        handler.LastCreateCommand.Metadata.TenantId.ShouldBe(Tenant);
    }

    /// <summary>
    /// Ensures caller metadata claiming a different tenant on the append-message route likewise cannot override the
    /// claims-derived tenant binding. The provenance-not-authority invariant holds across every command surface.
    /// </summary>
    [Fact]
    public async Task CallerMetadataSpoofingOnAppendRouteShouldNotOverrideClaimsDerivedTenant()
    {
        FakeCommandHandler handler = new();
        using WebApplication app = BuildApp(handler);

        CallerMetadata spoofingCaller = new(
            SchemaVersion.Current,
            "adopter-client",
            Origin: "adopter-portal",
            IntegrationContext: "elevated-origin");

        AppendMessageCommand command = new(
            new ConversationCommandMetadata(SchemaVersion.Current, Tenant, Actor, "corr-001", "cause-001", "idem-001"),
            Conversation,
            new MessageId("message-001"),
            Actor,
            "Hello from the adopter.",
            CallerMetadata: spoofingCaller);

        ApiResponse response = await InvokeAsync(
            app,
            "/api/v1/conversations/{conversationId}/messages",
            command,
            AuthenticatedUser(Tenant),
            new Dictionary<string, string?> { ["conversationId"] = Conversation.Value });

        response.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
        handler.LastAppendCommand.ShouldNotBeNull();

        // Trusted tenant binding stays the claims-derived tenant on the append route too.
        handler.LastAppendCommand!.Metadata.TenantId.ShouldBe(Tenant);
        handler.LastAppendCommand.Metadata.TenantId.ShouldNotBe(OtherTenant);
    }

    /// <summary>
    /// Ensures the published transport metadata for a message-appended event likewise carries no caller-supplied
    /// provenance value (only safe correlation/causation), confirming no-leak holds beyond conversation creation.
    /// </summary>
    [Fact]
    public void AppendedTransportMetadataShouldNotCarryCallerSuppliedProvenanceValues()
    {
        MessageAppended e = new(
            new ConversationEventMetadata(
                SchemaVersion.Current,
                "event-appended-001",
                ConversationEventType.MessageAppended,
                Tenant,
                Conversation,
                "correlation-001",
                new DateTimeOffset(2026, 5, 18, 11, 2, 0, TimeSpan.Zero),
                Actor,
                "causation-001"),
            new MessageId("message-001"),
            Actor,
            "Hello from the adopter.",
            PublicationSamples.ProviderCorrelation);

        ConversationTransportMetadata metadata = ConversationTransportMetadata.FromEvent(e);

        string combined = string.Join(
            '|',
            [metadata.Topic, metadata.Type, metadata.Source, metadata.Subject, .. metadata.Headers.Keys, .. metadata.Headers.Values]);

        metadata.Headers["correlationId"].ShouldBe("correlation-001");
        metadata.Headers["causationId"].ShouldBe("causation-001");

        combined.ShouldNotContain("adopter-client", Case.Insensitive);
        combined.ShouldNotContain("front-composer", Case.Insensitive);
        combined.ShouldNotContain("adopter-portal", Case.Insensitive);
        combined.ShouldNotContain("callerMetadata", Case.Insensitive);
    }

    private static WebApplication BuildApp(IConversationCommandApiHandler handler)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(handler);

        WebApplication app = builder.Build();
        app.MapConversationCommandApi();
        return app;
    }

    private static async Task<ApiResponse> InvokeAsync<T>(
        WebApplication app,
        string routePattern,
        T body,
        ClaimsPrincipal user,
        IReadOnlyDictionary<string, string?>? routeValues = null)
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => string.Equals(e.RoutePattern.RawText, routePattern, StringComparison.Ordinal));

        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            User = user,
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Headers[ConversationReadApi.CorrelationIdHeaderName] = "corr-001";

        if (routeValues is not null)
        {
            foreach (KeyValuePair<string, string?> routeValue in routeValues)
            {
                context.Request.RouteValues[routeValue.Key] = routeValue.Value;
            }
        }

        byte[] requestBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, JsonOptions));
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = new MemoryStream(requestBody);
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        string responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        return new ApiResponse(context.Response.StatusCode, responseBody);
    }

    private static ClaimsPrincipal AuthenticatedUser(TenantId tenantId)
        => new(new ClaimsIdentity(
            [new Claim(ConversationReadApi.TenantIdClaimType, tenantId.Value), new Claim(ClaimTypes.NameIdentifier, "caller-001")],
            authenticationType: "Test"));

    private static ConversationCreatedResult CreatedResult()
        => new(
            SchemaVersion.Current,
            Tenant,
            Conversation,
            "corr-001",
            "idem-001",
            new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up."),
            ConversationCommandType.CreateConversationCommand);

    private sealed record ApiResponse(int StatusCode, string Body);

    private sealed class FakeCommandHandler : IConversationCommandApiHandler
    {
        public CreateConversationCommand? LastCreateCommand { get; private set; }

        public AppendMessageCommand? LastAppendCommand { get; private set; }

        public ConversationCommandApiOutcome<ConversationCreatedResult>? CreateOutcome { get; init; }

        public ValueTask<ConversationCommandApiOutcome<ConversationCreatedResult>> CreateConversationAsync(
            CreateConversationCommand command,
            CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;
            return ValueTask.FromResult(CreateOutcome ?? ConversationCommandApiOutcome<ConversationCreatedResult>.Success(
                CreatedResult(),
                StatusCodes.Status201Created));
        }

        public ValueTask<ConversationCommandApiOutcome<ConversationCommandAcceptedResult>> AppendMessageAsync(
            AppendMessageCommand command,
            CancellationToken cancellationToken = default)
        {
            LastAppendCommand = command;
            return ValueTask.FromResult(ConversationCommandApiOutcome<ConversationCommandAcceptedResult>.Success(
                new ConversationCommandAcceptedResult(
                    SchemaVersion.Current,
                    Tenant,
                    Conversation,
                    ConversationCommandType.AppendMessageCommand,
                    "corr-001",
                    "idem-001",
                    new ReadModelVisibility(ProjectionTrustState.Rebuilding, "Read model is catching up.")),
                StatusCodes.Status202Accepted));
        }
    }
}
