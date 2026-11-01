// <copyright file="FakeLookupIssuer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.Conversations.Server.Tests.Agents;

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
