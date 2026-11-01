// <copyright file="DeletionRuntimeTransport.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

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
