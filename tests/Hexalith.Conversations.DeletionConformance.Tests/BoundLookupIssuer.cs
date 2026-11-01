// <copyright file="BoundLookupIssuer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Produces unsigned bound assertions for the lookup workload; never a production credential.</summary>
/// <param name="workload">The lookup workload identity.</param>
internal sealed class BoundLookupIssuer(string workload) : IWorkloadAssertionIssuer
{
    /// <inheritdoc />
    public bool CanBindResources => true;

    /// <inheritdoc />
    public ValueTask<string?> IssueAsync(WorkloadAssertionRequest request, CancellationToken cancellationToken = default)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = workload,
            [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = request.Operation,
        };
        foreach (KeyValuePair<string, string> binding in request.Bindings ?? new Dictionary<string, string>())
        {
            claims[binding.Key] = binding.Value;
        }

        return ValueTask.FromResult<string?>(new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = request.Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(2),
        }));
    }
}
