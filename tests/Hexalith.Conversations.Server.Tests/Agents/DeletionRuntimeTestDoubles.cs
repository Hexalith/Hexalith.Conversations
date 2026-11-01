// <copyright file="DeletionRuntimeTestDoubles.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Synthetic installation values for the opt-in deletion runtime host; never enrolled or qualified identities.</summary>
internal static class DeletionRuntimeTestSettings
{
    public const string Target = "0123456789abcdef0123456789abcdef01234567";
    public const string RolloverTarget = "89abcdef0123456789abcdef0123456789abcdef";
    public const string Worker = "conversations-worker";
    public const string Receiver = "conversations-deletion-conformance";
    public const string Lookup = "conversations-deletion-lookup";
    public const string Reader = "conversations-runtime-reader";
    public const string Platform = "eventstore";
    public const string Installation = "installation-a";
    public const string Storage = "conversations-storage";
    public const string ServiceParty = "conversations-dedicated-worker";
    public const string Gateway = "https://gateway.test/";
    public const string Sidecar = "http://127.0.0.1:3500";
    public const string SidecarToken = "sidecar-api-token";
    public const string WorkerToken = "worker-authority-token";
    public const string WorkerSecret = "worker-secret-value";
    public const string LookupSecret = "lookup-secret-value";
    public const string SigningKey = "deletion-runtime-test-signing-key-0123456789";

    /// <summary>Complete settings with a Development-only symmetric shared contract, for real-pipeline tests.</summary>
    public static Dictionary<string, string?> SymmetricConfiguration()
    {
        Dictionary<string, string?> settings = Configuration();
        settings.Remove("Authentication:JwtBearer:Authority");
        settings.Remove("Authentication:JwtBearer:AllowedAlgorithms:0");
        settings["Authentication:JwtBearer:SigningKey"] = SigningKey;
        return settings;
    }

    /// <summary>Signs a caller token with the Development symmetric contract.</summary>
    public static string CallerToken(string subject, string issuer = "https://authority.test/", IDictionary<string, object>? claims = null)
    {
        var all = new Dictionary<string, object>(claims ?? new Dictionary<string, object>(), StringComparer.Ordinal) { ["sub"] = subject };
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = "eventstore",
            Claims = all,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
    public const string TokenEndpoint = "https://authority.test/connect/token";

    public static ConversationDeletionRuntimeOptions Options() => new()
    {
        Enabled = true,
        TenantId = F.Tenant.Value,
        WorkerPrincipalId = Worker,
        ServicePartyId = ServiceParty,
        NamespaceInstallationId = Installation,
        StorageIdentity = Storage,
        GatewayBaseAddress = Gateway,
        PlatformWorkloadId = Platform,
        ReceiverPrincipalId = Receiver,
        LookupPrincipalId = Lookup,
        IdentityReaderPrincipalId = Reader,
        AcceptedTargetVersions = [Target, RolloverTarget],
    };

    public static Dictionary<string, string?> Configuration(bool enabled = true)
    {
        const string s = ConversationDeletionRuntimeOptions.SectionName;
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [ConversationProjectionStoreOptions.ConfigurationKey] = "deletion-runtime-state",
            ["EventStore:DataProtection:StateStoreName"] = "deletion-runtime-keys",
            ["EventStore:DataProtection:PersistToStateStore"] = "false",
            ["Tenants:PubSubName"] = "deletion-runtime-events",
            ["Tenants:TopicName"] = "deletion-runtime-tenants",
            ["Dapr:DeclarativeSubscriptions"] = "true",
        };
        if (!enabled)
        {
            return settings;
        }

        settings[s + ":Enabled"] = "true";
        settings[s + ":TenantId"] = F.Tenant.Value;
        settings[s + ":WorkerPrincipalId"] = Worker;
        settings[s + ":ServicePartyId"] = ServiceParty;
        settings[s + ":NamespaceInstallationId"] = Installation;
        settings[s + ":StorageIdentity"] = Storage;
        settings[s + ":GatewayBaseAddress"] = Gateway;
        settings[s + ":PlatformWorkloadId"] = Platform;
        settings[s + ":ReceiverPrincipalId"] = Receiver;
        settings[s + ":LookupPrincipalId"] = Lookup;
        settings[s + ":IdentityReaderPrincipalId"] = Reader;
        settings[s + ":AcceptedTargetVersions:0"] = Target;
        settings[s + ":AcceptedTargetVersions:1"] = RolloverTarget;
        settings["EventStore:Authentication:Authority"] = "https://authority.test/";
        settings["EventStore:Authentication:TokenEndpoint"] = TokenEndpoint;
        settings["EventStore:Authentication:ClientId"] = "worker-client";
        settings["EventStore:Authentication:ClientSecret"] = WorkerSecret;
        settings["Authentication:JwtBearer:Authority"] = "https://authority.test/";
        settings["Authentication:JwtBearer:Issuer"] = "https://authority.test/";
        settings["Authentication:JwtBearer:Audience"] = "eventstore";
        settings["Authentication:JwtBearer:AllowedAlgorithms:0"] = "RS256";
        settings[s + ":LookupIssuer:ClientId"] = "lookup-client";
        settings[s + ":LookupIssuer:ClientSecret"] = LookupSecret;
        settings[s + ":LookupIssuer:TokenEndpoint"] = TokenEndpoint;
        settings[s + ":LookupIssuer:EnableResourceBindingRequests"] = "true";
        settings["DAPR_HTTP_ENDPOINT"] = Sidecar;
        settings["DAPR_API_TOKEN"] = SidecarToken;
        return settings;
    }

    /// <summary>Approves one deletion on serialized local source events and projects its publication entry.</summary>
    public static async Task<ConversationDeletionDeliveryPumpFixture> ApprovedSourceAsync()
    {
        var f = new F();
        var before = await f.ReplayAsync();
        f.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"),
            F.Conversation, "independent-approval", before.SourceRevision, F.At.AddMinutes(1),
            F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
        var stream = (await f.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value),
            TestContext.Current.CancellationToken)).Stream!;
        return new(f, new(1, new ConversationDeletionPublicationProjector().Project(stream, TestContext.Current.CancellationToken).Single()));
    }

    public static ConversationDeletionSignal Signal(string id = "signal-alpha")
        => new(id, F.Tenant, F.Conversation, new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value).ActorId, 3,
            "independent-approval");

    public static string Evidence(ConversationDeletionSignal signal, string target)
        => ConformanceDeletionWireEvidencePrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            signal.ConversationDeletionSignalId + ":" + target)));

    public const string ConformanceDeletionWireEvidencePrefix = "oq-conformance-v1:";
}

/// <summary>Records formatted log entries so tests can assert value-free reason codes.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, int EventId, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, int EventId, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries)
        {
            _entries.Add((logLevel, eventId.Id, formatter(state, exception)));
        }
    }
}

/// <summary>Mirror of the receiver tool's command and lookup payload, used only to read what the adapter sends.</summary>
internal sealed record ToolConformanceDeletion(ConversationDeletionSignal Signal, string DeliveryAttemptId, string TargetVersion);

/// <summary>Records one stubbed HTTP exchange without any credential value leaving the test.</summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? SidecarToken, string? Assertion, string Body);

/// <summary>A shared primary handler that records requests and returns scripted responses.</summary>
internal sealed class StubHttpHandler(Func<HttpRequestMessage, string, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(),
            Header(request, "dapr-api-token"), Header(request, EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName), body);
        lock (_requests)
        {
            _requests.Add(recorded);
        }

        HttpResponseMessage response = await respond(request, body);
        response.RequestMessage ??= request;
        return response;
    }

    // Shared by every client pipeline in a composed host; the test owns its lifetime.
    protected override void Dispose(bool disposing)
    {
    }

    private static string? Header(HttpRequestMessage request, string name)
        => request.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : null;
}

/// <summary>Creates clients over one stub handler.</summary>
internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public List<string> Names { get; } = [];

    public HttpClient CreateClient(string name)
    {
        Names.Add(name);
        return new HttpClient(handler, disposeHandler: false);
    }
}

/// <summary>Replaces every composed client's primary handler after its own configuration, keeping all delegating handlers.</summary>
internal sealed class PrimaryHandlerOverride(HttpMessageHandler handler) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        => builder =>
        {
            next(builder);
            builder.PrimaryHandler = handler;
        };
}

/// <summary>A fake lookup issuer producing unsigned assertions with the requested scope; never a production credential.</summary>
internal sealed class FakeLookupIssuer(string workload = DeletionRuntimeTestSettings.Lookup) : IWorkloadAssertionIssuer
{
    private readonly List<WorkloadAssertionRequest> _requests = [];

    public bool CanBindResources { get; set; } = true;

    public bool OmitBindings { get; set; }

    public bool ReturnNothing { get; set; }

    public IReadOnlyList<WorkloadAssertionRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
    {
        lock (_requests)
        {
            _requests.Add(request);
        }

        if (ReturnNothing)
        {
            return ValueTask.FromResult<string?>(null);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = workload,
            [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = request.Operation,
        };
        if (!OmitBindings && request.Bindings is not null)
        {
            foreach (KeyValuePair<string, string> binding in request.Bindings)
            {
                claims[binding.Key] = binding.Value;
            }
        }

        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = request.Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(2),
        });
        return ValueTask.FromResult<string?>(token);
    }
}

/// <summary>Records SDK gateway submissions; it never confirms a receipt.</summary>
internal sealed class RecordingGateway : IEventStoreGatewayClient
{
    public List<SubmitCommandRequest> Commands { get; } = [];

    public Exception? Failure { get; set; }

    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
    {
        Commands.Add(request);
        return Failure is null
            ? Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId, MessageId: request.MessageId))
            : Task.FromException<SubmitCommandResponse>(Failure);
    }

    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Out-of-scope approval verifier double; denies every approval.</summary>
internal sealed class DeniedApprovalVerifier : IConversationDeletionApprovalVerifier
{
    public Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, ApproveConversationDeletionCommand command,
        CancellationToken cancellationToken = default) => Task.FromResult(ConversationAgentsOutcome.Denied);
}

/// <summary>Out-of-scope namespace source double; never certifies coverage.</summary>
internal sealed class AbsentNamespaceSource : ISourcePublicationNamespaceSource
{
    public Task<SourcePublicationCut?> ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult<SourcePublicationCut?>(null);
}

/// <summary>Out-of-scope index store double; never persists.</summary>
internal sealed class AbsentIndexStore : ISourcePublicationIndexStore
{
    public Task<SourcePublicationIndexState?> ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult<SourcePublicationIndexState?>(null);

    public Task<bool> TryWriteAsync(SourcePublicationScope scope, long expectedRevision, SourcePublicationIndexState state,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}

/// <summary>Out-of-scope authority double returning one fixed classified result.</summary>
internal sealed class StaticAuthority(ConversationAgentAuthorization result) : IConversationAgentAuthority
{
    public List<(string Principal, TenantId Tenant, ConversationId? Conversation, string Operation)> Calls { get; } = [];

    public Exception? Failure { get; set; }

    public Task<ConversationAgentAuthorization> AuthorizeAsync(string authenticatedPrincipalId, TenantId tenantId,
        ConversationId? conversationId, string operation, CancellationToken cancellationToken = default)
    {
        Calls.Add((authenticatedPrincipalId, tenantId, conversationId, operation));
        return Failure is null ? Task.FromResult(result) : Task.FromException<ConversationAgentAuthorization>(Failure);
    }
}

/// <summary>Receiver double returning one fixed current target.</summary>
internal sealed class StaticReceiver(string? target) : IConversationDeletionReceiver
{
    public Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken cancellationToken = default) => Task.FromResult(target);

    public Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

/// <summary>Scripted token authority, EventStore gateway, Conversations source and independent conformance receiver behind one primary handler.</summary>
/// <remarks>The gateway delegates Conversations source reads and delivery records to serialized local aggregate events. Receiver
/// submission records acceptance; only a later sidecar receipt query returns it. No production qualification is implied.</remarks>
internal sealed class DeletionRuntimeTransport
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ConversationDeletionDeliveryPumpFixture? _source;
    private readonly Dictionary<string, ConversationDeletionAcknowledgement> _accepted = new(StringComparer.Ordinal);
    private readonly List<ToolConformanceDeletion> _submissions = [];
    private readonly List<StreamReadRequest> _streamReads = [];
    private readonly List<StubHttpHandler> _invocationHandlers = [];

    public DeletionRuntimeTransport(ConversationDeletionDeliveryPumpFixture? source = null)
    {
        _source = source;
        Handler = new StubHttpHandler(RespondAsync);
    }

    public StubHttpHandler Handler { get; }

    /// <summary>Creates a separately owned invocation transport over the same scripted receiver.</summary>
    public HttpMessageHandler CreateInvocationHandler()
    {
        var handler = new StubHttpHandler(RespondAsync);
        _invocationHandlers.Add(handler);
        return handler;
    }

    private IEnumerable<RecordedRequest> Requests => Handler.Requests.Concat(_invocationHandlers.SelectMany(handler => handler.Requests));

    public string CurrentTarget { get; set; } = DeletionRuntimeTestSettings.Target;

    public bool ReceiverQueryFails { get; set; }

    /// <summary>Gets or sets a value indicating whether receipts carry an undeclared field, a malformed answer.</summary>
    public bool MalformedReceipt { get; set; }

    /// <summary>Gets or sets a value indicating whether receipts are well-formed but report no persisted deletion.</summary>
    public bool MismatchedReceipt { get; set; }

    public IReadOnlyList<StreamReadRequest> StreamReads
    {
        get
        {
            lock (_streamReads)
            {
                return [.. _streamReads];
            }
        }
    }

    public IReadOnlyList<ToolConformanceDeletion> Submissions
    {
        get
        {
            lock (_submissions)
            {
                return [.. _submissions];
            }
        }
    }

    public ConversationDeletionAcknowledgement? Accepted(string signalId)
    {
        lock (_accepted)
        {
            return _accepted.GetValueOrDefault(signalId);
        }
    }

    public IEnumerable<RecordedRequest> GatewayRequests => Requests.Where(static request => request.Uri.Host == "gateway.test");

    public IEnumerable<RecordedRequest> SidecarRequests => Requests.Where(static request => request.Uri.Port == 3500);

    private static HttpResponseMessage Json(HttpStatusCode status, object value)
        => new(status) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };

    private async Task<HttpResponseMessage> RespondAsync(HttpRequestMessage request, string body)
    {
        Uri uri = request.RequestUri!;
        if (uri.AbsoluteUri == DeletionRuntimeTestSettings.TokenEndpoint)
        {
            return Json(HttpStatusCode.OK, new { access_token = DeletionRuntimeTestSettings.WorkerToken, token_type = "Bearer", expires_in = 300 });
        }

        if (uri.Host == "gateway.test" && uri.AbsolutePath == "/api/v1/commands")
        {
            return await CommandAsync(body);
        }

        if (uri.Host == "gateway.test" && uri.AbsolutePath == "/api/v1/queries")
        {
            return await QueryAsync(body);
        }

        if (uri.Host == "gateway.test" && uri.AbsolutePath == "/api/v1/streams/read")
        {
            return StreamRead(body);
        }

        return uri.Port == 3500 && uri.AbsolutePath == "/v1.0/invoke/" + DeletionRuntimeTestSettings.Receiver + "/method/query"
            ? ReceiverQuery(body)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> CommandAsync(string body)
    {
        SubmitCommandRequest command = JsonSerializer.Deserialize<SubmitCommandRequest>(body, Web)!;
        if (command.Domain == "conversation" && command.CommandType == "RecordConversationDeletionDelivery" && _source is not null)
        {
            var delivery = command.Payload.GetProperty("publicCommand").Deserialize<RecordConversationDeletionDeliveryCommand>(Web)!;
            _ = await _source.RecordConversationDeletionDeliveryAsync(delivery, CancellationToken.None);
        }
        else if (command.Domain == ConversationDeletionRuntimeOptions.ReceiverDomain
            && command.CommandType == "Hexalith.Conversations.DeletionConformance.AcceptConformanceDeletion"
            && command.Tenant == F.Tenant.Value)
        {
            var submission = command.Payload.Deserialize<ToolConformanceDeletion>(Web)!;
            command.AggregateId.ShouldBe(submission.Signal.ConversationDeletionSignalId);
            lock (_submissions)
            {
                _submissions.Add(submission);
            }

            lock (_accepted)
            {
                _accepted[submission.Signal.ConversationDeletionSignalId] = new(submission.Signal.ConversationDeletionSignalId,
                    submission.Signal.SourceRevision, 19, submission.TargetVersion,
                    DeletionRuntimeTestSettings.Evidence(submission.Signal, submission.TargetVersion));
            }
        }
        else
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        return Json(HttpStatusCode.Accepted, new { correlationId = command.MessageId, messageId = command.MessageId });
    }

    private async Task<HttpResponseMessage> QueryAsync(string body)
    {
        SubmitQueryRequest query = JsonSerializer.Deserialize<SubmitQueryRequest>(body, Web)!;
        if (_source is null || query.Domain != "conversation" || query.QueryType != "conversation-deletion-source" || query.Payload is null)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        var request = query.Payload.Value.Deserialize<ConversationDeletionSourceQuery>(Web)!;
        ConversationDeletionSourceResult result = await _source.GetConversationDeletionSourceAsync(request, CancellationToken.None);
        return Json(HttpStatusCode.OK, new SubmitQueryResponse("source-correlation", JsonSerializer.SerializeToElement(result, Web)));
    }

    private HttpResponseMessage StreamRead(string body)
    {
        // A complete-looking page that carries no native source proof.
        StreamReadRequest request = JsonSerializer.Deserialize<StreamReadRequest>(body, Web)!;
        lock (_streamReads)
        {
            _streamReads.Add(request);
        }

        return Json(HttpStatusCode.OK, new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
            new StreamReadMetadata(request.FromSequence, request.ToSequence, null, 2, 0, false, null)));
    }

    private HttpResponseMessage ReceiverQuery(string body)
    {
        QueryEnvelope envelope = JsonSerializer.Deserialize<QueryEnvelope>(body, Web)!;
        if (ReceiverQueryFails)
        {
            return Json(HttpStatusCode.OK, QueryResult.Failure("receiver-query-unavailable"));
        }

        if (envelope.QueryType == "deletion-conformance-identity")
        {
            return Json(HttpStatusCode.OK, QueryResult.FromPayload(JsonSerializer.SerializeToElement(new
            {
                schemaVersion = 1,
                receiverKind = "ContractConformance",
                qualificationScope = "OQ-5.4-1",
                evidenceLevel = 4,
                agentsRuntimeEnabled = false,
                tenantId = envelope.TenantId,
                targetVersion = CurrentTarget,
                storageIdentity = "receiver-storage",
                receiverPrincipalId = DeletionRuntimeTestSettings.Receiver,
                lookupPrincipalId = envelope.UserId,
            })));
        }

        var lookup = JsonSerializer.Deserialize<ToolConformanceDeletion>(envelope.Payload, Web)!;
        ConversationDeletionAcknowledgement? accepted = Accepted(lookup.Signal.ConversationDeletionSignalId);
        object payload;
        if (accepted is null || accepted.TargetVersion != lookup.TargetVersion)
        {
            payload = new { outcome = 1, acknowledgement = (ConversationDeletionAcknowledgement?)null };
        }
        else if (MalformedReceipt)
        {
            JsonObject malformed = JsonSerializer.SerializeToNode(accepted, Web)!.AsObject();
            malformed["note"] = "undeclared";
            payload = new { outcome = 0, acknowledgement = malformed };
        }
        else
        {
            payload = new { outcome = 0, acknowledgement = MismatchedReceipt ? accepted with { ProtectedDeletionRevision = 0 } : accepted };
        }

        return Json(HttpStatusCode.OK, QueryResult.FromPayload(JsonSerializer.SerializeToElement(payload, Web)));
    }
}
