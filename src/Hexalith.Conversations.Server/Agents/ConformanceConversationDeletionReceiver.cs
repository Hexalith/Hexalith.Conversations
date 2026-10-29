// <copyright file="ConformanceConversationDeletionReceiver.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Adapts the separately installed OQ-5.4-1 contract-conformance receiver; gateway admission is never a receipt.</summary>
/// <remarks>Submission uses the SDK gateway client and its worker authority transport. Current-target and receipt lookups use the
/// host's own Dapr sidecar with its API token and a lookup assertion from the separately configured lookup issuer. The receiver
/// tool and Agents packages are never referenced; only the wire contract is mirrored. Each unavailable cause logs one stable
/// reason code and never a token, assertion, payload or configuration value.</remarks>
public sealed partial class ConformanceConversationDeletionReceiver : IConversationDeletionReceiver
{
    /// <summary>The named HTTP client for sidecar lookups; redirects are disabled.</summary>
    public const string LookupClientName = "Hexalith.Conversations.DeletionReceiverLookup";

    /// <summary>The keyed-service key of the separately configured lookup workload-assertion issuer.</summary>
    public const string LookupIssuerKey = "Hexalith.Conversations.DeletionReceiverLookupIssuer";

    private const int MaximumResponseBytes = 64 * 1024;
    private const string CurrentTargetOperation = "current-target";
    private const string LookupOperation = "receipt-lookup";
    private const string SubmissionOperation = "submission";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { AllowDuplicateProperties = false };

    private static readonly JsonDocumentOptions Document = new() { AllowDuplicateProperties = false, MaxDepth = 16 };

    private readonly IEventStoreGatewayClient _gateway;
    private readonly IHttpClientFactory _clients;
    private readonly IWorkloadAssertionIssuer _lookupIssuer;
    private readonly ConversationDeletionRuntimeOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConformanceConversationDeletionReceiver> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConformanceConversationDeletionReceiver"/> class.</summary>
    /// <param name="gateway">The SDK gateway client using the worker authority transport.</param>
    /// <param name="clients">The HTTP client factory supplying the redirect-free sidecar client.</param>
    /// <param name="lookupIssuer">The separately configured lookup workload-assertion issuer.</param>
    /// <param name="options">The validated installation references.</param>
    /// <param name="configuration">The host configuration holding the sidecar endpoint and API token.</param>
    /// <param name="logger">Receives value-free reason codes.</param>
    public ConformanceConversationDeletionReceiver(IEventStoreGatewayClient gateway, IHttpClientFactory clients,
        [FromKeyedServices(LookupIssuerKey)] IWorkloadAssertionIssuer lookupIssuer, ConversationDeletionRuntimeOptions options,
        IConfiguration configuration, ILogger<ConformanceConversationDeletionReceiver> logger)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(lookupIssuer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        _gateway = gateway;
        _clients = clients;
        _lookupIssuer = lookupIssuer;
        _options = options;
        _configuration = configuration;
        _logger = logger;
    }

    private static ConversationDeletionReceiverResult Unavailable { get; } = new(ConversationAgentsOutcome.Unavailable);

    /// <inheritdoc />
    public async Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tenant is null || tenant.Value != _options.TenantId)
        {
            LogUnavailable(_logger, CurrentTargetOperation, "request-inadmissible");
            return null;
        }

        byte[]? payload = await QueryAsync(CurrentTargetOperation, ConformanceDeletionWire.IdentityAggregateId,
            ConformanceDeletionWire.IdentityQueryType, [], cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return payload is null ? null : ReadIdentity(payload);
    }

    /// <inheritdoc />
    public async Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Admissible(signal, attemptId, target))
        {
            LogUnavailable(_logger, LookupOperation, "request-inadmissible");
            return Unavailable;
        }

        byte[] request = JsonSerializer.SerializeToUtf8Bytes(new ConformanceDeletionRequest(signal, attemptId, target), Json);
        byte[]? payload = await QueryAsync(LookupOperation, signal.ConversationDeletionSignalId, ConformanceDeletionWire.ReceiptQueryType,
            request, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return payload is null ? Unavailable : ReadReceipt(payload, signal, target);
    }

    /// <inheritdoc />
    /// <remarks>A gateway 202 is admission only, so this never returns Available; the pump confirms through independent lookup.</remarks>
    public async Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Admissible(signal, attemptId, target))
        {
            LogUnavailable(_logger, SubmissionOperation, "request-inadmissible");
            return Unavailable;
        }

        string messageId = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            "conversation-deletion-conformance-submission-v1", signal.ConversationDeletionSignalId, attemptId, target,
        })));
        var command = new SubmitCommandRequest(messageId, _options.TenantId, ConversationDeletionRuntimeOptions.ReceiverDomain,
            signal.ConversationDeletionSignalId, ConformanceDeletionWire.CommandType,
            JsonSerializer.SerializeToElement(new ConformanceDeletionRequest(signal, attemptId, target), Json), messageId,
            IdempotencyKey: messageId);
        try
        {
            _ = await _gateway.SubmitCommandAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is EventStoreGatewayException
            or HttpRequestException or InvalidOperationException or JsonException or IOException or OperationCanceledException)
        {
            // Transport failure is unavailable; independent lookup still decides whether a receipt exists.
            LogUnavailable(_logger, SubmissionOperation, "submission-transport-failed");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Unavailable;
    }

    /// <summary>Resolves this host's own sidecar: HTTPS, or HTTP to a loopback host, with an empty path.</summary>
    /// <param name="configuration">The configuration holding <c>DAPR_HTTP_ENDPOINT</c> or <c>DAPR_HTTP_PORT</c>.</param>
    /// <param name="sidecar">The sidecar origin.</param>
    /// <returns>True for an acceptable sidecar origin.</returns>
    internal static bool TryResolveSidecar(IConfiguration configuration, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? sidecar)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        sidecar = null;
        string? endpoint = configuration["DAPR_HTTP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            if (!int.TryParse(configuration["DAPR_HTTP_PORT"], NumberStyles.None, CultureInfo.InvariantCulture, out int port)
                || port is < 1 or > 65535)
            {
                return false;
            }

            endpoint = "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);
        }

        // Plain HTTP would carry the sidecar token and assertion in cleartext off-host.
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            || !(uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/")
        {
            return false;
        }

        sidecar = uri;
        return true;
    }

    private bool Admissible(ConversationDeletionSignal? signal, string? attemptId, string? target)
    {
        try
        {
            return signal is { TenantId: not null, ConversationId: not null } && signal.TenantId.Value == _options.TenantId
                && signal.SourceRevision > 0 && signal.SourceContractVersion == 1
                && !string.IsNullOrWhiteSpace(signal.ApprovalReference) && signal.ApprovalReference.Length <= 1024
                && new AggregateIdentity(_options.TenantId, ConversationDeletionRuntimeOptions.ReceiverDomain,
                    signal.ConversationDeletionSignalId).AggregateId == signal.ConversationDeletionSignalId
                && new AggregateIdentity(_options.TenantId, "conversation", signal.ConversationId.Value).ActorId == signal.SourceStream
                && !string.IsNullOrWhiteSpace(attemptId) && attemptId.Length <= 256 && _options.AcceptsTarget(target);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private async Task<byte[]?> QueryAsync(string operation, string aggregateId, string queryType, byte[] payload,
        CancellationToken cancellationToken)
    {
        if (!_lookupIssuer.CanBindResources)
        {
            LogUnavailable(_logger, operation, "issuer-cannot-bind");
            return null;
        }

        if (!TryResolveSidecar(_configuration, out Uri? sidecar))
        {
            LogUnavailable(_logger, operation, "sidecar-unconfigured");
            return null;
        }

        if (_configuration["DAPR_API_TOKEN"] is not { } channelToken || string.IsNullOrWhiteSpace(channelToken))
        {
            LogUnavailable(_logger, operation, "sidecar-token-missing");
            return null;
        }

        string appId = _options.ReceiverPrincipalId;
        var bindings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = _options.TenantId,
            [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = ConversationDeletionRuntimeOptions.ReceiverDomain,
        };
        try
        {
            string? assertion = await _lookupIssuer.IssueAsync(
                new WorkloadAssertionRequest(appId, EventStoreWorkloadOperations.DomainServiceQuery, bindings), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(assertion))
            {
                LogUnavailable(_logger, operation, "assertion-unavailable");
                return null;
            }

            if (!BoundLookupAssertion(assertion, appId, bindings))
            {
                LogUnavailable(_logger, operation, "assertion-unbound");
                return null;
            }

            var envelope = new QueryEnvelope(_options.TenantId, ConversationDeletionRuntimeOptions.ReceiverDomain, aggregateId, queryType,
                payload, Guid.NewGuid().ToString("N"), _options.LookupPrincipalId);
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(sidecar, "/v1.0/invoke/" + Uri.EscapeDataString(appId) + "/method/query"))
            {
                Content = JsonContent.Create(envelope, options: Json),
            };
            request.Headers.Add("dapr-api-token", channelToken);
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            HttpClient client = _clients.CreateClient(LookupClientName);
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogHttpStatus(_logger, operation, (int)response.StatusCode);
                return null;
            }

            byte[]? body = response.Content.Headers.ContentLength is > MaximumResponseBytes
                ? null : await ReadBoundedAsync(response.Content, cancellationToken).ConfigureAwait(false);
            if (body is null)
            {
                LogUnavailable(_logger, operation, "response-oversized");
                return null;
            }

            QueryResult? result = JsonSerializer.Deserialize<QueryResult>(body, Json);
            if (result is { Success: true, PayloadBytes: { Length: > 0 and <= MaximumResponseBytes } bytes })
            {
                return bytes;
            }

            LogUnavailable(_logger, operation, "query-failed");
            return null;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is JsonException or FormatException
            or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            LogUnavailable(_logger, operation, "payload-malformed");
            return null;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is HttpRequestException or IOException
            or OperationCanceledException)
        {
            LogUnavailable(_logger, operation, "transport-failed");
            return null;
        }
    }

    private bool BoundLookupAssertion(string assertion, string audience, IReadOnlyDictionary<string, string> bindings)
    {
        if (assertion.Length > EventStoreWorkloadAuthenticationDefaults.MaximumAssertionLength
            || JwtWorkloadAssertionIssuer.GetTokenScopeFailure(assertion, audience, EventStoreWorkloadOperations.DomainServiceQuery) is not null)
        {
            return false;
        }

        var token = new JsonWebToken(assertion);
        string[] callers = [.. token.Claims.Where(static claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.CallerClaimType)
            .Select(static claim => claim.Value)];
        return callers.Length == 1 && callers[0] == _options.LookupPrincipalId
            && bindings.All(binding => token.Claims.Where(claim => claim.Type == binding.Key).Select(static claim => claim.Value)
                .SequenceEqual([binding.Value]));
    }

    private string? ReadIdentity(byte[] payload)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload, Document);
            if (!TryReadObject(document.RootElement, out Dictionary<string, JsonElement>? fields)
                || fields.Count != ConformanceDeletionWire.IdentityFields.Count
                || !ConformanceDeletionWire.IdentityFields.All(fields.ContainsKey))
            {
                LogUnavailable(_logger, CurrentTargetOperation, "payload-malformed");
                return null;
            }

            if (!fields.TryGetValue("schemaVersion", out JsonElement schema) || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out int schemaVersion) || schemaVersion != 1
                || Text(fields, "receiverKind") != ConformanceDeletionWire.ReceiverKind
                || Text(fields, "qualificationScope") != ConformanceDeletionWire.QualificationScope
                || !fields.TryGetValue("evidenceLevel", out JsonElement level) || level.ValueKind != JsonValueKind.Number
                || !level.TryGetInt32(out int evidenceLevel) || evidenceLevel != ConformanceDeletionWire.EvidenceLevel
                || !fields.TryGetValue("agentsRuntimeEnabled", out JsonElement agents) || agents.ValueKind != JsonValueKind.False
                || Text(fields, "tenantId") != _options.TenantId
                || string.IsNullOrWhiteSpace(Text(fields, "storageIdentity"))
                || Text(fields, "receiverPrincipalId") != _options.ReceiverPrincipalId
                || Text(fields, "lookupPrincipalId") != _options.LookupPrincipalId)
            {
                LogUnavailable(_logger, CurrentTargetOperation, "identity-mismatch");
                return null;
            }

            string? target = Text(fields, "targetVersion");
            if (_options.AcceptsTarget(target))
            {
                return target;
            }

            LogUnavailable(_logger, CurrentTargetOperation, "target-not-accepted");
            return null;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // Invalid UTF-8 text surfaces as InvalidOperationException from JsonElement.GetString.
            LogUnavailable(_logger, CurrentTargetOperation, "payload-malformed");
            return null;
        }
    }

    private ConversationDeletionReceiverResult ReadReceipt(byte[] payload, ConversationDeletionSignal signal, string target)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload, Document);
            if (!TryReadObject(document.RootElement, out Dictionary<string, JsonElement>? fields)
                || !fields.TryGetValue("outcome", out JsonElement outcomeElement)
                || fields.Keys.Any(static key => !ConformanceDeletionWire.ReceiptFields.Contains(key)))
            {
                LogUnavailable(_logger, LookupOperation, "payload-malformed");
                return Unavailable;
            }

            if (!TryReadOutcome(outcomeElement, out ConversationAgentsOutcome outcome))
            {
                LogUnavailable(_logger, LookupOperation, "outcome-unknown");
                return Unavailable;
            }

            ConversationDeletionAcknowledgement? acknowledgement = null;
            if (fields.TryGetValue("acknowledgement", out JsonElement acknowledgementElement)
                && acknowledgementElement.ValueKind != JsonValueKind.Null
                && !TryReadAcknowledgement(acknowledgementElement, out acknowledgement))
            {
                LogUnavailable(_logger, LookupOperation, "payload-malformed");
                return Unavailable;
            }

            switch (outcome)
            {
                case ConversationAgentsOutcome.Available when acknowledgement is null:
                    LogUnavailable(_logger, LookupOperation, "receipt-missing");
                    return Unavailable;
                case ConversationAgentsOutcome.Available when Exact(acknowledgement, signal, target):
                    return new(outcome, acknowledgement);
                case ConversationAgentsOutcome.Available:
                    // A well-formed authenticated receipt with changed same-id fields is an integrity conflict for quarantine.
                    LogConflict(_logger, LookupOperation, "receipt-conflict");
                    return new(ConversationAgentsOutcome.Conflict);
                case ConversationAgentsOutcome.Absent or ConversationAgentsOutcome.Conflict when acknowledgement is not null:
                    LogUnavailable(_logger, LookupOperation, "payload-ambiguous");
                    return Unavailable;
                case ConversationAgentsOutcome.Conflict:
                    LogConflict(_logger, LookupOperation, "receiver-conflict");
                    return new(outcome);
                case ConversationAgentsOutcome.Absent:
                    return new(outcome);
                default:
                    LogUnavailable(_logger, LookupOperation, "receiver-outcome");
                    return Unavailable;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException
            or InvalidOperationException)
        {
            // Invalid UTF-8 text surfaces as InvalidOperationException from JsonElement.GetString.
            LogUnavailable(_logger, LookupOperation, "payload-malformed");
            return Unavailable;
        }
    }

    private static bool TryReadAcknowledgement(JsonElement element,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ConversationDeletionAcknowledgement? acknowledgement)
    {
        acknowledgement = null;
        if (!TryReadObject(element, out Dictionary<string, JsonElement>? fields)
            || fields.Count != ConformanceDeletionWire.AcknowledgementFields.Count
            || !ConformanceDeletionWire.AcknowledgementFields.All(fields.ContainsKey)
            || Text(fields, "signalId") is not { } signalId || Text(fields, "targetVersion") is not { } targetVersion
            || Text(fields, "evidence") is not { } evidence
            || fields["sourceRevision"].ValueKind != JsonValueKind.Number || !fields["sourceRevision"].TryGetInt64(out long sourceRevision)
            || fields["protectedDeletionRevision"].ValueKind != JsonValueKind.Number
            || !fields["protectedDeletionRevision"].TryGetInt64(out long protectedRevision))
        {
            return false;
        }

        acknowledgement = new(signalId, sourceRevision, protectedRevision, targetVersion, evidence);
        return true;
    }

    private static bool Exact(ConversationDeletionAcknowledgement? receipt, ConversationDeletionSignal signal, string target)
        => receipt is not null && receipt.SignalId == signal.ConversationDeletionSignalId && receipt.SourceRevision == signal.SourceRevision
            && receipt.ProtectedDeletionRevision > 0 && receipt.TargetVersion == target
            && receipt.Evidence is { Length: 82 } evidence && evidence.StartsWith(ConformanceDeletionWire.EvidencePrefix, StringComparison.Ordinal)
            && evidence[ConformanceDeletionWire.EvidencePrefix.Length..].All(char.IsAsciiHexDigit);

    private static bool TryReadOutcome(JsonElement element, out ConversationAgentsOutcome outcome)
    {
        outcome = ConversationAgentsOutcome.Unavailable;
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (!element.TryGetInt32(out int number) || !Enum.IsDefined((ConversationAgentsOutcome)number))
            {
                return false;
            }

            outcome = (ConversationAgentsOutcome)number;
            return true;
        }

        return element.ValueKind == JsonValueKind.String && element.GetString() is { Length: > 0 } name
            && name.All(char.IsAsciiLetter) && Enum.TryParse(name, ignoreCase: true, out outcome) && Enum.IsDefined(outcome);
    }

    private static bool TryReadObject(JsonElement element, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Dictionary<string, JsonElement>? fields)
    {
        fields = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var read = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            // Web deserialization is case-insensitive, so case variants of one name are ambiguous.
            if (!read.TryAdd(property.Name, property.Value))
            {
                return false;
            }
        }

        fields = new Dictionary<string, JsonElement>(read, StringComparer.Ordinal);
        return fields.Count == read.Count;
    }

    private static string? Text(Dictionary<string, JsonElement> fields, string name)
        => fields.TryGetValue(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static async Task<byte[]?> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) != 0)
        {
            if (buffer.Length + count > MaximumResponseBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, count);
        }

        return buffer.ToArray();
    }

    [LoggerMessage(EventId = 54111, Level = LogLevel.Warning,
        Message = "Conversation deletion receiver {Operation} is unavailable: {Reason}")]
    private static partial void LogUnavailable(ILogger logger, string operation, string reason);

    [LoggerMessage(EventId = 54112, Level = LogLevel.Warning,
        Message = "Conversation deletion receiver {Operation} is unavailable: http-status {StatusCode}")]
    private static partial void LogHttpStatus(ILogger logger, string operation, int statusCode);

    [LoggerMessage(EventId = 54113, Level = LogLevel.Error,
        Message = "Conversation deletion receiver {Operation} reported a conflict: {Reason}")]
    private static partial void LogConflict(ILogger logger, string operation, string reason);
}
