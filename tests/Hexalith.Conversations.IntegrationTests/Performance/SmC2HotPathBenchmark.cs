// <copyright file="SmC2HotPathBenchmark.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Idempotency;
using Hexalith.Conversations.State;

namespace Hexalith.Conversations.IntegrationTests.Performance;

/// <summary>
/// Captures the frozen SM-C2 warm-path workload using stable in-process Conversations domain and read shapes.
/// </summary>
/// <remarks>
/// The fixture deliberately reports raw per-operation samples. Release evidence binds those samples to the
/// source revision and runtime envelope; the fixture itself does not rewrite committed evidence artifacts.
/// </remarks>
public sealed class SmC2HotPathBenchmark
{
    private const int OperationsPerSample = 2_000;
    private const int Repetitions = 30;
    private const int WarmupRepetitions = 5;

    private static readonly TenantId Tenant = new("tenant-sm-c2");
    private static readonly ConversationId Conversation = new("conversation-sm-c2");
    private static readonly PartyId Actor = new("party-sm-c2");
    private static readonly MessageId Message = new("message-sm-c2");
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Emits one raw sample set for every frozen warm-path inventory row.
    /// </summary>
    [Fact]
    public void FrozenWarmPathInventoryEmitsComparableRawSamples()
    {
        CreateConversation create = CreateCommand();
        AppendMessageCommand append = AppendCommand("SM-C2 payload");
        AppendMessageCommand duplicate = AppendCommand("SM-C2 payload");
        AppendMessageCommand mismatch = AppendCommand("SM-C2 changed payload");
        ConversationState state = CreatedState(create);
        IReadOnlyDictionary<string, ConversationState> details =
            new Dictionary<string, ConversationState>(StringComparer.Ordinal) { [Conversation.Value] = state };
        IReadOnlyList<string> summaries = Enumerable.Range(0, 100)
            .Select(index => $"conversation-{index:000}")
            .ToArray();

        Dictionary<string, double[]> samples = new(StringComparer.Ordinal)
        {
            ["HP-CREATE"] = Measure(() => ConversationAggregate.Handle(create, state: null)),
            ["HP-APPEND"] = Measure(() =>
            {
                ConversationCommandFingerprint accepted = ConversationCommandFingerprint.Create(append, Conversation);
                ConversationCommandFingerprint replay = ConversationCommandFingerprint.Create(duplicate, Conversation);
                ConversationCommandFingerprint rejected = ConversationCommandFingerprint.Create(mismatch, Conversation);
                return accepted == replay && accepted != rejected;
            }),
            ["HP-LIST"] = Measure(() => summaries
                .Where(static value => value.StartsWith("conversation-", StringComparison.Ordinal))
                .OrderBy(static value => value, StringComparer.Ordinal)
                .Take(25)
                .ToArray()),
            ["HP-OPEN"] = Measure(() => details.TryGetValue(Conversation.Value, out ConversationState? value)
                ? (value.ConversationId, value.TenantId, value.Lifecycle, value.Messages.Count)
                : default),
        };

        samples.Keys.OrderBy(static value => value, StringComparer.Ordinal)
            .ShouldBe(["HP-APPEND", "HP-CREATE", "HP-LIST", "HP-OPEN"]);
        samples.ShouldAllBe(static row => row.Value.Length == Repetitions && row.Value.All(value => value > 0));

        foreach ((string hotPathId, double[] raw) in samples.OrderBy(static row => row.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"SM-C2|{hotPathId}|raw-microseconds={string.Join(',', raw.Select(static value => value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)))}|p95-microseconds={Percentile95(raw):F6}");
        }
    }

    private static AppendMessageCommand AppendCommand(string text)
        => new(
            Metadata("idempotency-append"),
            Conversation,
            Message,
            Actor,
            text);

    private static ConversationState CreatedState(CreateConversation create)
    {
        ConversationCreatedDomainEvent created = ConversationAggregate.Handle(create, state: null)
            .Events
            .Single()
            .ShouldBeOfType<ConversationCreatedDomainEvent>();
        var state = new ConversationState();
        state.Apply(created);
        return state;
    }

    private static CreateConversation CreateCommand()
        => new(
            new CreateConversationCommand(
                Metadata("idempotency-create"),
                new BusinessReference("crm", "case-sm-c2"),
                new ProjectId("project-sm-c2"),
                new FolderId("folder-sm-c2"),
                "SM-C2 conversation"),
            Conversation,
            Timestamp,
            "event-sm-c2-create");

    private static ConversationCommandMetadata Metadata(string idempotencyKey)
        => new(
            SchemaVersion.Current,
            Tenant,
            Actor,
            "correlation-sm-c2",
            "causation-sm-c2",
            idempotencyKey);

    private static double[] Measure<T>(Func<T> operation)
    {
        for (int repetition = 0; repetition < WarmupRepetitions; repetition++)
        {
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(operation());
            }
        }

        var samples = new double[Repetitions];
        for (int repetition = 0; repetition < Repetitions; repetition++)
        {
            long started = Stopwatch.GetTimestamp();
            for (int operationIndex = 0; operationIndex < OperationsPerSample; operationIndex++)
            {
                GC.KeepAlive(operation());
            }

            samples[repetition] = Stopwatch.GetElapsedTime(started).TotalMicroseconds / OperationsPerSample;
        }

        return samples;
    }

    private static double Percentile95(IEnumerable<double> samples)
    {
        double[] ordered = [.. samples.Order()];
        int index = (int)Math.Ceiling(0.95 * ordered.Length) - 1;
        return ordered[Math.Max(index, 0)];
    }
}
