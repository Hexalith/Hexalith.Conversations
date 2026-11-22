using Hexalith.Conversations.Api;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.AddDefaultHealthChecks();
const string section = "Authentication:JwtBearer";
var contract = builder.Configuration.GetSection(section).Get<JwtBearerAuthenticationOptions>() ?? new();
var validation = JwtBearerAuthenticationContract.Validate(contract, builder.Environment, section);
if (validation.Failed)
    throw new OptionsValidationException(section, typeof(JwtBearerAuthenticationOptions), validation.Failures);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => JwtBearerAuthenticationContract.Configure(options, contract));
builder.Services.AddEventStoreHttpHostSecurity();
builder.Services.AddHttpContextAccessor();
if (!ConformanceConversationDeletionReceiver.TryResolveSidecar(builder.Configuration, out var sidecar))
    throw new InvalidOperationException("The Conversations API requires its Dapr HTTP sidecar endpoint.");
builder.Services.AddTransient<HumanGatewayBearerHandler>();
builder.Services.AddEventStoreGatewayClient(options =>
    {
        options.BaseAddress = sidecar;
        // Dedicated human action paths keep Dapr grants disjoint from workload routes.
        options.CommandPath = "api/v1/commands/submit";
        options.QueryPath = "api/v1/queries/submit";
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    .AddHttpMessageHandler<HumanGatewayBearerHandler>()
    .AddEventStoreDaprServiceInvocation("eventstore", builder.Configuration["DAPR_API_TOKEN"]);
var app = builder.Build();
app.UseRouting();
app.UseEventStoreHttpHostSecurity();
app.UseAuthentication();
app.UseAuthorization();
var group = app.MapGroup("/api/v1/conversations").RequireAuthorization(EventStoreHttpHostSecurityExtensions.HumanPolicy);
group.MapPost("/", PilotCommands.CreateAsync);
group.MapPost("/{conversationId}/messages", PilotCommands.AppendAsync);
group.MapGet("/{conversationId}", PilotQueries.DetailAsync);
group.MapGet("/", PilotQueries.ListAsync);
app.MapDefaultEndpoints();
app.Run();
