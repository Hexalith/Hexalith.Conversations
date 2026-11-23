using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

static string Required(string key) => Environment.GetEnvironmentVariable(key) is { Length: > 0 } value
    ? value : throw new InvalidOperationException($"Set {key} before starting the journey.");
static void Check(bool condition, string step)
{
    if (!condition) throw new InvalidOperationException($"Journey failed at {step}.");
}
static Uri Origin(string key)
{
    var uri = new Uri(Required(key));
    Check(uri.IsAbsoluteUri && (uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback)
        && uri.PathAndQuery == "/" && uri.Fragment == "" && uri.UserInfo == "", key);
    return uri;
}
static HttpClient Transport(Uri origin, string? bearer = null, WireErrors? errors = null)
{
    var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
    if (errors is not null) errors.InnerHandler = handler;
    var client = new HttpClient((HttpMessageHandler?)errors ?? handler) { BaseAddress = origin };
    if (bearer != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
    return client;
}
static T Success<T>(ConversationClientResult<T> result, string operation) where T : class =>
    result.Value ?? throw new InvalidOperationException($"{operation} failed (HTTP {(int?)result.StatusCode}, {result.Error?.Errors.FirstOrDefault()?.Code}).");
static string Segment(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

var origin = Origin("CONVERSATIONS_ORIGIN");
var stateOrigin = Origin("CONVERSATIONS_DOMAIN_DAPR_ORIGIN");
Check(stateOrigin.IsLoopback && origin != stateOrigin, "independent loopback state reader");
var run = Guid.NewGuid().ToString("N");
var tenant = new TenantId(Required("CONVERSATIONS_TENANT"));
var otherTenant = new TenantId(Required("CONVERSATIONS_OTHER_TENANT"));
var subject = Required("CONVERSATIONS_SUBJECT");
var otherSubject = Required("CONVERSATIONS_OTHER_SUBJECT");
var party = new PartyId(Required("CONVERSATIONS_PARTY"));
var bearer = Required("CONVERSATIONS_TOKEN");
var otherBearer = Required("CONVERSATIONS_OTHER_TOKEN");
Check(tenant != otherTenant && subject != otherSubject && bearer != otherBearer, "two independent identities");
var wire = new WireErrors();
var otherWire = new WireErrors();
using var http = Transport(origin, bearer, wire);
using var foreignHttp = Transport(origin, otherBearer, otherWire);
using var anonymous = Transport(origin);
using var state = Transport(stateOrigin);
if (Environment.GetEnvironmentVariable("CONVERSATIONS_DOMAIN_DAPR_TOKEN") is { Length: > 0 } stateToken)
    state.DefaultRequestHeaders.Add("dapr-api-token", stateToken);
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4));
var token = deadline.Token;
var client = new ConversationClient(http);
var context = new ConversationClientContext(tenant, party, subject, run, IdempotencyKey: $"create-{run}");
var business = new BusinessReference("client-journey", run);
var create = new CreateConversationCommand(context.ToCommandMetadata(), business, Label: $"Private {run}");
async Task<ConversationId> VerifyCreate(CreateConversationCommand command, string step)
{
    var result = await client.CreateConversationAsync(command, token);
    var accepted = Success(result, step);
    Check(result.StatusCode == HttpStatusCode.Created && accepted.TenantId == tenant
        && accepted.CommandType == ConversationCommandType.CreateConversationCommand
        && accepted.SchemaVersion == SchemaVersion.Current && accepted.Visibility.State == ProjectionTrustState.Rebuilding
        && accepted.IdempotencyKey == command.Metadata.IdempotencyKey && accepted.CorrelationId == run, step);
    return accepted.ConversationId;
}
var id = await VerifyCreate(create, "create receipt");
Check(await VerifyCreate(create, "create retry receipt") == id, "create retry identity");
var createConflict = await client.CreateConversationAsync(create with { Label = $"Changed {run}" }, token);
Check(createConflict.StatusCode == HttpStatusCode.Conflict && !createConflict.IsSuccess, "create changed intent");
wire.Verify(HttpStatusCode.Conflict, ConversationErrorCode.IdempotencyConflict, run);
var append = new AppendMessageCommand((context with { IdempotencyKey = $"append-{run}" }).ToCommandMetadata(),
    id, new MessageId(run), party, $"Ordinary message {run}", OperationTimestamp: DateTimeOffset.UtcNow.AddDays(7));
var store = Uri.EscapeDataString(Required("CONVERSATIONS_READ_MODEL_STORE"));
async Task<ConversationDetailProjectionV1?> Durable(TenantId scope, ConversationId conversation, CancellationToken readToken)
{
    var stateKey = Uri.EscapeDataString($"projection:conversations:{Segment(scope.Value)}:{Segment(conversation.Value)}");
    using var response = await state.GetAsync($"v1.0/state/{store}/{stateKey}?consistency=strong", readToken);
    if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound) return null;
    response.EnsureSuccessStatusCode();
    using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(readToken));
    return doc.RootElement.GetProperty("detail").Deserialize<ConversationDetailProjectionV1>(JsonSerializerOptions.Web);
}
async Task VerifyAppend(AppendMessageCommand command, string step)
{
    var result = await client.AppendMessageAsync(command, token);
    var accepted = Success(result, step);
    Check(result.StatusCode == HttpStatusCode.Accepted && accepted.ConversationId == command.ConversationId && accepted.TenantId == tenant
        && accepted.CommandType == ConversationCommandType.AppendMessageCommand
        && accepted.SchemaVersion == SchemaVersion.Current && accepted.Visibility.State == ProjectionTrustState.Rebuilding
        && accepted.IdempotencyKey == command.Metadata.IdempotencyKey && accepted.CorrelationId == run, step);
}
var missingKeyAppend = append with
{
    MessageId = new MessageId($"missing-key-{run}"),
    Metadata = (context with { IdempotencyKey = null }).ToCommandMetadata(),
};
var missing = await client.AppendMessageAsync(missingKeyAppend, token);
Check(missing.StatusCode == HttpStatusCode.BadRequest && !missing.IsSuccess
    && missing.Error?.Errors.Any(error => error.Code == ConversationErrorCode.IdempotencyKeyMissing && error.CorrelationId == run
        && !error.IsRetryable && error.ClientAction == ConversationErrorClientAction.ProvideIdempotencyKey) == true, "typed missing key");
wire.Verify(HttpStatusCode.BadRequest, ConversationErrorCode.IdempotencyKeyMissing, run);
var foreignAppend = append with
{
    MessageId = new MessageId($"foreign-{run}"),
    Metadata = (context with { IdempotencyKey = $"foreign-{run}" }).ToCommandMetadata(),
};
var denied = await new ConversationClient(foreignHttp).AppendMessageAsync(foreignAppend, token);
Check(denied.StatusCode == HttpStatusCode.Forbidden && !denied.IsSuccess, "foreign write");
otherWire.Verify(HttpStatusCode.Forbidden, ConversationErrorCode.TenantContextMismatch, run);

if (Environment.GetEnvironmentVariable("CONVERSATIONS_STATUS_FAILURE_ORIGIN") is not null)
{
    var interruption = Origin("CONVERSATIONS_STATUS_FAILURE_ORIGIN");
    Check(interruption.IsLoopback && interruption != origin, "local status interruption");
    var outageCreate = new CreateConversationCommand((context with { IdempotencyKey = $"outage-create-{run}" }).ToCommandMetadata(),
        new BusinessReference("client-journey-outage", run), Label: $"Outage {run}");
    var outageId = await VerifyCreate(outageCreate, "create outage conversation");
    var outageAppend = append with
    {
        ConversationId = outageId,
        MessageId = new MessageId($"outage-{run}"),
        Metadata = (context with { IdempotencyKey = $"outage-append-{run}" }).ToCommandMetadata(),
    };
    var interruptedWire = new WireErrors();
    using var interrupted = Transport(interruption, bearer, interruptedWire);
    var outcome = await new ConversationClient(interrupted).AppendMessageAsync(outageAppend, token);
    Check(outcome.StatusCode == HttpStatusCode.ServiceUnavailable && !outcome.IsSuccess, $"unconfirmed append (HTTP {(int?)outcome.StatusCode}, {outcome.Error?.Errors.FirstOrDefault()?.Code})");
    interruptedWire.Verify(HttpStatusCode.ServiceUnavailable, ConversationErrorCode.IdempotencyOutcomeUnknown, run);
    using var commitDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
    commitDeadline.CancelAfter(TimeSpan.FromSeconds(60));
    while ((await Durable(tenant, outageId, commitDeadline.Token))?.Messages.Any(m => m.MessageId == outageAppend.MessageId) != true)
        await Task.Delay(250, commitDeadline.Token);
    await VerifyAppend(outageAppend, "outage append retry receipt");
    var afterRetry = await Durable(tenant, outageId, token);
    Check(afterRetry?.Messages is [{ MessageId: var persistedId }] && persistedId == outageAppend.MessageId,
        "one durable outage message after replay");
}
await VerifyAppend(append, "append receipt");
await VerifyAppend(append, "append retry receipt");
var conflict = await client.AppendMessageAsync(append with { Text = $"Changed {run}" }, token);
Check(conflict.StatusCode == HttpStatusCode.Conflict && conflict.Error?.Errors.Any(e => e.Code == ConversationErrorCode.IdempotencyConflict) == true, "changed intent");
wire.Verify(HttpStatusCode.Conflict, ConversationErrorCode.IdempotencyConflict, run);
var readerContext = new ConversationClientContext(otherTenant, new PartyId(Required("CONVERSATIONS_OTHER_PARTY")), otherSubject, run,
    IdempotencyKey: $"reader-create-{run}");
var readerCreate = new CreateConversationCommand(readerContext.ToCommandMetadata(), business, Label: $"Reader {run}");
var readerDenied = await new ConversationClient(foreignHttp).CreateConversationAsync(readerCreate, token);
Check(readerDenied.StatusCode == HttpStatusCode.Forbidden && !readerDenied.IsSuccess, "tenant reader mutation");
otherWire.Verify(HttpStatusCode.Forbidden, ConversationErrorCode.TenantIsolationViolation, run);

var filter = new ConversationListFilterV1(BusinessReference: business);
ListConversationsQuery List(TenantId scope, string caller) => new(SchemaVersion.Current, scope, caller, run, filter);
void VerifyDetail(TenantId owner, ConversationId conversation, BusinessReference? reference, string? label,
    IReadOnlyList<ConversationTimelineMessageProjectionV1> messages)
{
    Check(owner == tenant && conversation == id && reference == business
        && label == create.Label && messages.Count == 1, "conversation and message count");
    var message = messages.Single();
    Check(message.CreatedAt < append.OperationTimestamp!.Value.AddDays(-1), "owner-assigned message occurrence");
    Check(message.MessageId == append.MessageId && message.AuthorPartyId == party && message.Text == append.Text, "message attribution and content");
}
using var visibility = CancellationTokenSource.CreateLinkedTokenSource(token);
visibility.CancelAfter(TimeSpan.FromSeconds(60));
string lastObservation = "No read completed.";
try
{
    while (true)
    {
        var detail = await client.GetConversationAsync(context.ToGetConversationQuery(id), visibility.Token);
        var list = await client.ListConversationsAsync(List(tenant, subject), visibility.Token);
        lastObservation = $"detail HTTP {(int?)detail.StatusCode} {detail.Value?.FreshnessState.Value} {detail.Value?.ReasonCode.Value}, "
            + $"list HTTP {(int?)list.StatusCode} {list.Value?.FreshnessState.Value} {list.Value?.ReasonCode.Value}";
        if (detail.StatusCode == HttpStatusCode.OK && list.StatusCode == HttpStatusCode.OK
            && detail.Value is { Details: { } projected, FreshnessState: var freshness } && freshness == ProjectionTrustState.Current
            && projected.Messages.Any(message => message.MessageId == append.MessageId)
            && list.Value?.FreshnessState == ProjectionTrustState.Current && list.Value.Conversations.Any(row => row.ConversationId == id && row.MessageCount == 1))
        {
            VerifyDetail(projected.TenantId, projected.ConversationId, projected.BusinessReference, projected.Label, projected.Messages);
            var row = list.Value.Conversations.Single();
            Check(row.ConversationId == id && row.TenantId == tenant && row.BusinessReference == business, "one Current list row");
            break;
        }
        await Task.Delay(250, visibility.Token);
    }
}
catch (OperationCanceledException) when (visibility.IsCancellationRequested && !token.IsCancellationRequested)
{
    throw new TimeoutException($"Current reads did not converge within 60 seconds. Last observation was {lastObservation}.");
}

var detailQuery = context.ToGetConversationQuery(id);
var membership = await client.GetConversationAsync(detailQuery with { NativeMembershipOnly = true }, token);
Check(membership.StatusCode == HttpStatusCode.OK && membership.Value?.ReadPurpose == GetConversationQuery.NativeMembershipPurpose
    && membership.Value.Details?.Messages.Count == 0, "bounded membership detail");
var advisory = await client.GetConversationAsync(detailQuery with { AdvisoryMetadataOnly = true }, token);
Check((advisory.StatusCode == HttpStatusCode.ServiceUnavailable && advisory.Value is { Details: null } unavailable
        && unavailable.FreshnessState == ProjectionTrustState.Unavailable)
    || (advisory.StatusCode == HttpStatusCode.OK && advisory.Value?.ReadPurpose == ConversationResolutionScoringLabel.SupportedPurpose
        && advisory.Value.Details?.Messages.Count == 0), "bounded advisory detail");

const string route = "api/v1/conversations";
var detailPath = $"{route}/{Uri.EscapeDataString(id.Value)}";
using (var response = await anonymous.GetAsync(detailPath, token))
    Check(response.StatusCode == HttpStatusCode.Unauthorized, "unauthenticated detail");
var json = JsonSerializerOptions.Web;
async Task Hidden(HttpClient transport, string tenantHeader, string principalHeader, string path, HttpStatusCode status, JsonNode? expected)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, path);
    request.Headers.Add("X-Tenant-Id", tenantHeader);
    request.Headers.Add("X-Caller-Principal-Id", principalHeader);
    using var response = await transport.SendAsync(request, token);
    var body = await response.Content.ReadAsStringAsync(token);
    Check(response.StatusCode == status && JsonNode.DeepEquals(JsonNode.Parse(body), expected), "exact hidden response");
}
var hiddenDetail = JsonSerializer.SerializeToNode(ConversationDetailResult.Hidden(SchemaVersion.Current), json);
var hiddenList = JsonSerializer.SerializeToNode(ConversationListResult.Hidden(SchemaVersion.Current), json);
foreach (var (transport, scope, caller) in new[] { (http, tenant.Value, otherSubject), (foreignHttp, tenant.Value, otherSubject) })
{
    await Hidden(transport, scope, caller, detailPath, HttpStatusCode.NotFound, hiddenDetail);
    await Hidden(transport, scope, caller, route, HttpStatusCode.OK, hiddenList);
}
await Hidden(foreignHttp, otherTenant.Value, otherSubject, detailPath, HttpStatusCode.NotFound, hiddenDetail);
var foreignList = Success(await new ConversationClient(foreignHttp).ListConversationsAsync(List(otherTenant, otherSubject), token), "other tenant list");
Check(foreignList.Conversations.Count == 0 && foreignList.FreshnessState == ProjectionTrustState.Current, "other tenant scoped list");

var durable = await Durable(tenant, id, token) ?? throw new InvalidOperationException("Missing persisted detail.");
Check(durable.Messages.All(message => message.MessageId != missingKeyAppend.MessageId && message.MessageId != foreignAppend.MessageId),
    "denied message IDs absent from owner's durable detail");
var otherDurable = await Durable(otherTenant, id, token);
Check(otherDurable?.Messages.Any(message => message.MessageId == foreignAppend.MessageId) != true,
    "foreign write absent from other tenant's durable detail");
VerifyDetail(durable.TenantId, durable.ConversationId, durable.BusinessReference, durable.Label, durable.Messages);


sealed class WireErrors : DelegatingHandler
{
    private HttpStatusCode? _status;
    private byte[]? _body;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        _body = null;
        _status = null;
        var response = await base.SendAsync(request, token);
        if (request.Method == HttpMethod.Post && !response.IsSuccessStatusCode)
        {
            _status = response.StatusCode;
            _body = await response.Content.ReadAsByteArrayAsync(token);
        }
        return response;
    }

    internal void Verify(HttpStatusCode status, ConversationErrorCode code, string correlation)
    {
        if (_status != status || _body is not { Length: > 0 }
            || JsonSerializer.Deserialize<ConversationErrorResult>(_body, JsonSerializerOptions.Web) is not { } envelope
            || !envelope.Errors.Any(error => error.Code == code && error.CorrelationId == correlation))
            throw new InvalidOperationException($"Missing HTTP {(int)status} wire error {code.Value} with the command correlation.");
    }
}
