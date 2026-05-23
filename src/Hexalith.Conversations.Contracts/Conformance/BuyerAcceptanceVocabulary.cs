// <copyright file="BuyerAcceptanceVocabulary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

[JsonConverter(typeof(BuyerAcceptanceItemStatusJsonConverter))]
public sealed record BuyerAcceptanceItemStatus
{
    public static BuyerAcceptanceItemStatus Accepted { get; } = new("accepted");
    public static BuyerAcceptanceItemStatus Excluded { get; } = new("excluded");
    public static BuyerAcceptanceItemStatus UnknownAccepted { get; } = new("unknown-accepted");
    public static BuyerAcceptanceItemStatus Waived { get; } = new("waived");

    private static readonly IReadOnlyDictionary<string, BuyerAcceptanceItemStatus> KnownValues = Known(
        Accepted, Excluded, UnknownAccepted, Waived);

    private BuyerAcceptanceItemStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    public string Value { get; }

    public static IReadOnlyList<BuyerAcceptanceItemStatus> All { get; } =
    [
        Accepted, Excluded, UnknownAccepted, Waived,
    ];

    public static BuyerAcceptanceItemStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(BuyerAcceptanceItemStatus));

    public override string ToString() => Value;
}
