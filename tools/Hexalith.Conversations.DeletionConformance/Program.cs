using Hexalith.Conversations.DeletionConformance;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.DomainService;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Deletion conformance qualification requires a non-Development environment.");
}
builder.AddEventStoreDomainService(typeof(DeletionConformanceAggregate).Assembly);
builder.Services.AddOptions<DeletionConformanceOptions>()
    .BindConfiguration(DeletionConformanceOptions.SectionName)
    .Validate(static options => options.IsConfigured(), "Separately enrolled qualification receiver bindings are required.")
    .Validate<IConfiguration>(static (options, configuration) => options.ReceiverPrincipalId == configuration["EventStore:DomainService:AppId"]
        && options.ReceiverPrincipalId == configuration["EventStore:Authentication:ClientId"]
        && configuration["EventStore:Authentication:GrantType"] is null or "client_credentials"
        && !string.IsNullOrWhiteSpace(configuration["EventStore:Authentication:ClientSecret"]),
        "The receiver requires its own application identity and authority client credentials.")
    .ValidateOnStart();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DeletionConformanceAuthorization>();
builder.Services.AddScoped<DeletionConformanceStreams>();
builder.Services.AddScoped<IDomainServiceAdmissionStage, DeletionConformanceAdmission>();
builder.Services.AddEventStoreGatewayClient()
    .ConfigureHttpClient((services, client) => client.BaseAddress = new Uri(services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeletionConformanceOptions>>().Value.GatewayBaseAddress))
    .AddEventStoreGatewayAuthorityTransport(builder.Configuration);
builder.Services.AddScoped<IAuthoritativeEventStreamReader>(services => new AuthoritativeEventStreamReader(
    services.GetRequiredService<IEventStoreGatewayClient>(), TimeProvider.System, readTimeout: null, requireNativeProof: true));
var app = builder.Build();
app.UseEventStoreDomainService();
app.Run();
