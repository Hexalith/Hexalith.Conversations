// <copyright file="EmptyHydrationResults.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.ObjectModel;

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Provides a shared empty read-only dictionary for hydration reference lookups with zero references.
/// </summary>
/// <typeparam name="TReference">The reference identifier type.</typeparam>
internal static class EmptyHydrationResults<TReference>
    where TReference : class
{
    /// <summary>Gets the shared empty hydration result dictionary.</summary>
    internal static IReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>> Instance { get; } =
        ReadOnlyDictionary<TReference, ReferenceHydrationResult<TReference>>.Empty;
}
