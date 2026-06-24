// <copyright file="ConversationStringValueJsonConverter.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.Serialization;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>
/// Thin local adapter over the promoted ruleless Commons string-value converter base
/// (FR-14 / Story 3.6, closing the Story 2.6 FR-8 deferral). The load-bearing token-type
/// guard, malformed-payload rejection, and round-trip skeleton now live once in
/// <see cref="StringValueJsonConverter{T}"/>; this adapter only preserves the
/// Conversation-prefixed derivation surface the closed-vocabulary converters bind to,
/// keeping the inventoried file path stable. Behavior is identical and pinned by
/// <c>GenericValueConverterSkeletonTest</c> and <c>ContractSerializationTest</c>.
/// </summary>
/// <typeparam name="T">The string-encoded value type.</typeparam>
internal abstract class ConversationStringValueJsonConverter<T> : StringValueJsonConverter<T>
    where T : notnull
{
}
