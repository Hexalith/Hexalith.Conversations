// <copyright file="SecondAdopterVocabulary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

[JsonConverter(typeof(SecondAdopterStatusJsonConverter))]
public sealed record SecondAdopterStatus
{
    public static SecondAdopterStatus Identified { get; } = new("identified");
    public static SecondAdopterStatus Qualified { get; } = new("qualified");
    public static SecondAdopterStatus Deferred { get; } = new("deferred");
    public static SecondAdopterStatus Disqualified { get; } = new("disqualified");

    private static readonly IReadOnlyDictionary<string, SecondAdopterStatus> KnownValues = Known(
        Identified, Qualified, Deferred, Disqualified);

    private SecondAdopterStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static IReadOnlyList<SecondAdopterStatus> All { get; } =
    [
        Identified, Qualified, Deferred, Disqualified,
    ];

    public static SecondAdopterStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(SecondAdopterStatus));

    public override string ToString() => Value;
}
