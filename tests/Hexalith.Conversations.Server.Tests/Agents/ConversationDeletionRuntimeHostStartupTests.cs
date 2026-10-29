// <copyright file="ConversationDeletionRuntimeHostStartupTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Server.Agents;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using S = Hexalith.Conversations.Server.Tests.Agents.DeletionRuntimeTestSettings;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Runs the real Server entry point: an enabled host without installed providers refuses before it listens.</summary>
public sealed class ConversationDeletionRuntimeHostStartupTests
{
    /// <summary>The production <c>Program</c> refuses startup naming the first missing out-of-scope provider; nothing is served.</summary>
    [Fact]
    public void EnabledHostWithoutInstalledProvidersRefusesBeforeListening()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach ((string key, string? value) in S.Configuration())
            {
                builder.UseSetting(key, value);
            }
        });

        Exception refusal = Should.Throw<Exception>(() => factory.CreateClient());

        Chain(refusal).ShouldContain(exception => exception is InvalidOperationException
            && exception.Message == "Authenticated Conversation runtime provider is missing: IConversationAgentAuthority.",
            refusal.ToString());
    }

    /// <summary>An enabled host with incomplete configuration refuses before any provider check.</summary>
    [Fact]
    public void EnabledHostWithIncompleteConfigurationRefusesBeforeListening()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach ((string key, string? value) in S.Configuration())
            {
                builder.UseSetting(key, key.EndsWith(":AcceptedTargetVersions:0", StringComparison.Ordinal) ? "main" : value);
            }
        });

        Exception refusal = Should.Throw<Exception>(() => factory.CreateClient());

        Chain(refusal).ShouldContain(exception => exception is InvalidOperationException
            && exception.Message.Contains(ConversationDeletionRuntimeOptions.SectionName + ":AcceptedTargetVersions", StringComparison.Ordinal),
            refusal.ToString());
    }

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
            if (current is AggregateException aggregate)
            {
                foreach (Exception inner in aggregate.InnerExceptions.SelectMany(Chain))
                {
                    yield return inner;
                }
            }
        }
    }
}
