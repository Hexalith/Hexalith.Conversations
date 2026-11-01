// <copyright file="RecordedRequest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Records one stubbed HTTP exchange without any credential value leaving the test.</summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? SidecarToken, string? Assertion, string Body);
