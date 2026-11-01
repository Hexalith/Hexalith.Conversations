// <copyright file="PrimaryHandlerOverride.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Extensions.Http;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Replaces every composed client's primary handler after its own configuration, keeping all delegating handlers.</summary>
internal sealed class PrimaryHandlerOverride(HttpMessageHandler handler) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        => builder =>
        {
            next(builder);
            builder.PrimaryHandler = handler;
        };
}
