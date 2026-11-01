// <copyright file="ConversationDeletionRuntimeHostExtensions.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Claims;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Api;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Opt-in composition of the Conversations-side deletion runtime pieces that need no independent authority.</summary>
/// <remarks>Out-of-scope providers (agent authority, approval verifier, publication namespace source and index store) stay
/// mandatory: they must be registered before this call, otherwise <see cref="ConversationAgentServiceCollectionExtensions.AddConversationDeletionRuntime"/>
/// refuses the host before it listens. Nothing here mints tokens, accepts caller-supplied state proofs or establishes enrollment.</remarks>
public static class ConversationDeletionRuntimeHostExtensions
{
    /// <summary>The authentication scheme validating inbound Bearer callers with the shared JWT contract.</summary>
    public const string CallerScheme = "ConversationDeletionRuntimeCaller";

    /// <summary>The authorization policy of the runtime identity and participant routes.</summary>
    public const string CallerPolicy = "ConversationDeletionRuntimeCaller";

    /// <summary>The shared public JWT contract section validating inbound callers.</summary>
    public const string CallerContractSection = "Authentication:JwtBearer";

    private const string WorkerAuthoritySection = "EventStore:Authentication";

    /// <summary>Binds and validates the opt-in configuration, registers the in-scope adapters, then composes the deletion runtime.</summary>
    /// <param name="services">Host registrations, already holding any independently installed providers.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The registrations.</returns>
    /// <exception cref="InvalidOperationException">The configuration is incomplete, or a mandatory provider is missing.</exception>
    public static IServiceCollection AddConversationDeletionRuntimeHost(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(ConversationDeletionRuntimeOptions)))
        {
            throw new InvalidOperationException("The Conversation deletion runtime host is already composed.");
        }

        ConversationDeletionRuntimeOptions options = configuration.GetSection(ConversationDeletionRuntimeOptions.SectionName)
            .Get<ConversationDeletionRuntimeOptions>() ?? new();
        var failures = options.Validate().Select(static key => ConversationDeletionRuntimeOptions.SectionName + ":" + key).ToList();
        IConfigurationSection worker = configuration.GetSection(WorkerAuthoritySection);
        foreach (string key in new[] { "Authority", "TokenEndpoint", "ClientId", "ClientSecret" })
        {
            if (string.IsNullOrWhiteSpace(worker[key]))
            {
                failures.Add(WorkerAuthoritySection + ":" + key);
            }
        }

        if (worker["GrantType"] is not (null or "client_credentials"))
        {
            failures.Add(WorkerAuthoritySection + ":GrantType");
        }

        WorkloadAssertionIssuerOptions lookup = configuration.GetSection(ConversationDeletionRuntimeOptions.LookupIssuerSectionName)
            .Get<WorkloadAssertionIssuerOptions>() ?? new();
        if (!string.IsNullOrWhiteSpace(lookup.Workload) && lookup.Workload != options.LookupPrincipalId)
        {
            failures.Add(ConversationDeletionRuntimeOptions.LookupIssuerSectionName + ":Workload");
        }

        if (string.IsNullOrWhiteSpace(lookup.ClientId) || lookup.ClientId == worker["ClientId"]
            || lookup.ClientId == configuration[WorkloadAssertionIssuerOptions.SectionName + ":ClientId"])
        {
            failures.Add(ConversationDeletionRuntimeOptions.LookupIssuerSectionName + ":ClientId");
        }

        if (string.IsNullOrWhiteSpace(lookup.ClientSecret))
        {
            failures.Add(ConversationDeletionRuntimeOptions.LookupIssuerSectionName + ":ClientSecret");
        }

        if (string.IsNullOrWhiteSpace(lookup.TokenEndpoint))
        {
            failures.Add(ConversationDeletionRuntimeOptions.LookupIssuerSectionName + ":TokenEndpoint");
        }

        if (!lookup.EnableResourceBindingRequests)
        {
            failures.Add(ConversationDeletionRuntimeOptions.LookupIssuerSectionName + ":EnableResourceBindingRequests");
        }

        if (string.IsNullOrWhiteSpace(configuration["DAPR_API_TOKEN"]))
        {
            failures.Add("DAPR_API_TOKEN");
        }

        if (!ConformanceConversationDeletionReceiver.TryResolveSidecar(configuration, out _))
        {
            failures.Add("DAPR_HTTP_ENDPOINT|DAPR_HTTP_PORT");
        }

        if (failures.Count > 0 || !options.TryGetGateway(out Uri gateway))
        {
            throw new InvalidOperationException("The opt-in Conversation deletion runtime configuration is incomplete or invalid: "
                + string.Join(", ", failures) + ".");
        }

        // The lookup issuer is its own workload; it never borrows the host's or the worker's identity.
        lookup.Workload = options.LookupPrincipalId;
        JwtBearerAuthenticationOptions callerContract = configuration.GetSection(CallerContractSection)
            .Get<JwtBearerAuthenticationOptions>() ?? new();

        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(WorkloadSecurityClock.System);
        services.AddHttpContextAccessor();

        // SDK gateway client on the worker authority transport, and the SDK native-proof complete stream reader.
        services.AddEventStoreGatewayClient(gatewayOptions => gatewayOptions.BaseAddress = gateway)
            .ConfigurePrimaryHttpMessageHandler(CreateRedirectFreeHandler)
            .AddEventStoreGatewayAuthorityTransport(configuration);
        services.Replace(ServiceDescriptor.Scoped<IAuthoritativeEventStreamReader>(static provider => new AuthoritativeEventStreamReader(
            provider.GetRequiredService<IEventStoreGatewayClient>(), provider.GetRequiredService<TimeProvider>(), readTimeout: null,
            requireNativeProof: true)));

        // SDK complete-source admission; the adapter reads only the SDK-admitted prefix.
        services.AddEventStoreCompleteCommandSourceVerification(options.PlatformWorkloadId, ["conversation"]);
        services.Replace(ServiceDescriptor.Singleton<IConversationCommandSourceVerifier, CompleteSourceConversationCommandSourceVerifier>());

        // Receiver adapter: gateway submission, and lookups through this host's sidecar with the separate lookup issuer.
        services.AddHttpClient(ConformanceConversationDeletionReceiver.LookupClientName, static client => client.Timeout = TimeSpan.FromSeconds(15))
            .ConfigurePrimaryHttpMessageHandler(CreateRedirectFreeHandler);
        services.AddKeyedSingleton<IWorkloadAssertionIssuer>(ConformanceConversationDeletionReceiver.LookupIssuerKey,
            (provider, _) => new JwtWorkloadAssertionIssuer(
                provider.GetRequiredService<IOptionsMonitor<JwtBearerAuthenticationOptions>>(),
                new FixedOptionsMonitor<WorkloadAssertionIssuerOptions>(lookup),
                provider.GetRequiredService<IHostEnvironment>(),
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<WorkloadSecurityClock>(),
                provider.GetRequiredService<ILogger<JwtWorkloadAssertionIssuer>>()));
        services.AddScoped<IConversationDeletionReceiver, ConformanceConversationDeletionReceiver>();

        // Conversations client: the inbound Bearer inside a request, otherwise the worker authority token.
        services.TryAddTransient<ConversationRuntimeCallerTokenHandler>();
        services.AddHexalithConversationsClient(clientOptions => clientOptions.Endpoint = gateway)
            .ConfigurePrimaryHttpMessageHandler(CreateRedirectFreeHandler)
            .AddHttpMessageHandler<ConversationRuntimeCallerTokenHandler>()
            .AddEventStoreGatewayAuthorityTransport(configuration);

        // Inbound Bearer callers of the runtime identity and participant routes.
        services.AddAuthentication().AddJwtBearer(CallerScheme, static _ => { });
        services.AddOptions<JwtBearerOptions>(CallerScheme)
            .Configure<IHostEnvironment>((target, environment) => ConfigureCaller(target, callerContract, environment))
            .Validate<IHostEnvironment>((_, environment) => JwtBearerAuthenticationContract.Validate(callerContract, environment,
                CallerContractSection).Succeeded, CallerContractSection + " must be a valid shared JWT contract.")
            .ValidateOnStart();
        services.AddAuthorization(authorization => authorization.AddPolicy(CallerPolicy, policy => policy
            .AddAuthenticationSchemes(CallerScheme).RequireAuthenticatedUser().RequireClaim("sub")));

        return services.AddConversationDeletionRuntime(new ConversationTenantCatalogueRegistration(options.Scope),
            new ConversationDeletionWorkerRegistration(options.Tenant, options.WorkerPrincipalId, new PartyId(options.ServicePartyId)));
    }

    /// <summary>Maps the authenticated runtime identity and restricted participant routes of an opted-in host.</summary>
    /// <param name="endpoints">The host route builder.</param>
    /// <returns>The route builder.</returns>
    public static IEndpointRouteBuilder MapConversationDeletionRuntimeHost(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (endpoints.ServiceProvider.GetService<ConversationDeletionRuntimeOptions>() is null)
        {
            throw new InvalidOperationException("Map the deletion runtime only after AddConversationDeletionRuntimeHost.");
        }

        RouteGroupBuilder callers = endpoints.MapGroup(string.Empty);
        callers.RequireAuthorization(CallerPolicy);
        callers.MapConversationRuntimeIdentity();
        callers.MapConversationAgentParticipantApi(CallerPolicy);
        return endpoints;
    }

    private static void ConfigureCaller(JwtBearerOptions target, JwtBearerAuthenticationOptions contract, IHostEnvironment environment)
    {
        // Authority metadata and signing keys are fetched without following redirects.
        target.BackchannelHttpHandler = CreateRedirectFreeHandler();
        if (!JwtBearerAuthenticationContract.Validate(contract, environment, CallerContractSection).Succeeded)
        {
            return;
        }

        JwtBearerAuthenticationContract.Configure(target, contract);
        target.SaveToken = false;
        target.IncludeErrorDetails = false;
        target.Events = new JwtBearerEvents
        {
            OnTokenValidated = static context =>
            {
                // Handlers identify the caller by NameIdentifier; it is the validated token subject, never another identity.
                if (context.Principal?.Identity is not ClaimsIdentity identity
                    || identity.FindAll("sub").ToArray() is not [{ Value: { Length: > 0 } subject }]
                    || identity.HasClaim(static claim => claim.Type == ClaimTypes.NameIdentifier))
                {
                    context.Fail("The caller subject is absent or ambiguous.");
                    return Task.CompletedTask;
                }

                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
                return Task.CompletedTask;
            },
        };
    }

    private static HttpMessageHandler CreateRedirectFreeHandler() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false };

    private sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
