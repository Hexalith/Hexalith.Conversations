// <copyright file="DerivedKeyPriorTenantAccessProxy.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Dynamic proxy implementing the prior binary's <c>IConversationTenantAccessService</c> inside its isolated
/// <see cref="DerivedKeyPriorBinaryLoadContext"/>.
/// </summary>
internal class DerivedKeyPriorTenantAccessProxy : DispatchProxy
{
    private HashSet<TenantId> _allowedTenants = [];
    private Type _decisionType = null!;
    private Type _denialReasonType = null!;

    /// <summary>
    /// Creates an allow-list proxy for the supplied prior-binary <c>IConversationTenantAccessService</c> interface.
    /// </summary>
    /// <param name="interfaceType">The prior binary's tenant-access interface type.</param>
    /// <param name="decisionType">The prior binary's decision type.</param>
    /// <param name="denialReasonType">The prior binary's denial-reason enum type.</param>
    /// <param name="allowedTenants">Tenants authorized for this test host.</param>
    /// <returns>A proxy instance implementing <paramref name="interfaceType"/>.</returns>
    internal static object Create(
        Type interfaceType,
        Type decisionType,
        Type denialReasonType,
        params TenantId[] allowedTenants)
    {
        MethodInfo createMethod = typeof(DispatchProxy)
            .GetMethod(nameof(Create), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
            .MakeGenericMethod(interfaceType, typeof(DerivedKeyPriorTenantAccessProxy));
        object proxyObj = createMethod.Invoke(null, null)!;
        DerivedKeyPriorTenantAccessProxy proxy = (DerivedKeyPriorTenantAccessProxy)proxyObj;
        proxy._allowedTenants = [.. allowedTenants];
        proxy._decisionType = decisionType;
        proxy._denialReasonType = denialReasonType;
        return proxyObj;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        if (targetMethod.DeclaringType == typeof(object))
        {
            return targetMethod.Name switch
            {
                nameof(ToString) => nameof(DerivedKeyPriorTenantAccessProxy),
                nameof(GetHashCode) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                nameof(Equals) => ReferenceEquals(this, args?[0]),
                _ => targetMethod.Invoke(this, args),
            };
        }

        object?[] parameters = args ?? [];
        object requirement = parameters[0]!;
        TenantId? trustedTenantId = parameters[1] as TenantId;
        string? callerPrincipalId = parameters[2] as string;
        TenantId? routeTenantId = parameters[3] as TenantId;
        TenantId? commandTenantId = parameters[4] as TenantId;
        TenantId? aggregateTenantId = parameters[5] as TenantId;
        TenantId? projectionTenantId = parameters[6] as TenantId;
        TenantId? idempotencyTenantId = parameters[7] as TenantId;

        bool allowed = trustedTenantId is not null
            && !string.IsNullOrWhiteSpace(callerPrincipalId)
            && _allowedTenants.Contains(trustedTenantId)
            && (routeTenantId is null || routeTenantId == trustedTenantId)
            && (commandTenantId is null || commandTenantId == trustedTenantId)
            && (aggregateTenantId is null || aggregateTenantId == trustedTenantId)
            && (projectionTenantId is null || projectionTenantId == trustedTenantId)
            && (idempotencyTenantId is null || idempotencyTenantId == trustedTenantId);

        object decision;
        if (allowed)
        {
            decision = _decisionType.GetMethod("Allowed", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [requirement, trustedTenantId, callerPrincipalId, null, null])!;
        }
        else
        {
            object unauthorized = Enum.Parse(_denialReasonType, "UnauthorizedTenant");
            decision = _decisionType.GetMethod("Denied", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [requirement, trustedTenantId, callerPrincipalId, unauthorized, false, null, null])!;
        }

        Type valueTaskType = typeof(ValueTask<>).MakeGenericType(_decisionType);
        return Activator.CreateInstance(valueTaskType, decision);
    }
}
