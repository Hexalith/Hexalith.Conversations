// <copyright file="ConversationIntValueJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.Serialization;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>
/// Thin local adapter over the promoted ruleless Commons integer-value converter base
/// (FR-14 / Story 3.6, closing the Story 2.6 FR-8 deferral). The load-bearing integer
/// token guard (<c>TryGetInt32</c>, no fractional/exponent/string values), out-of-range
/// rejection, and round-trip skeleton now live once in <see cref="IntValueJsonConverter{T}"/>;
/// this adapter only preserves the Conversation-prefixed derivation surface
/// <c>SchemaVersionJsonConverter</c> binds to, keeping the inventoried file path stable.
/// Behavior is identical and pinned by <c>GenericValueConverterSkeletonTest</c> and
/// <c>ContractSerializationTest</c>.
/// </summary>
/// <typeparam name="T">The integer-encoded value type.</typeparam>
internal abstract class ConversationIntValueJsonConverter<T> : IntValueJsonConverter<T>
    where T : notnull
{
}
