// <copyright file="ConversationConformanceStatusClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded closed-vocabulary status class for conformance outcome observations.
/// </summary>
/// <remarks>
/// Values map conformance check outcomes and gate statuses to safe bounded-cardinality
/// telemetry dimensions. <see cref="None"/> is reserved for code initialization only and
/// must never be supplied to telemetry methods.
/// </remarks>
public enum ConversationConformanceStatusClass
{
    /// <summary>Default/unset value — throw <see cref="ArgumentException"/> if supplied to telemetry.</summary>
    None = 0,

    /// <summary>Conformance check passed: observed outcome matched the contract requirement.</summary>
    Pass = 1,

    /// <summary>Conformance check failed: a product invariant was violated.</summary>
    Fail = 2,

    /// <summary>Conformance gate is waived: a named waiver explicitly covers this gate (gate-level only).</summary>
    Waived = 3,

    /// <summary>Evidence is partial or deferred but has been accepted for this release.</summary>
    UnknownAccepted = 4,

    /// <summary>Infrastructure or platform fault — not a product defect.</summary>
    InfrastructureFailure = 5,

    /// <summary>Evidence is stale or degraded; authorized non-trust-bearing state was observed.</summary>
    StaleEvidence = 6,

    /// <summary>The conformance harness itself failed to execute the check.</summary>
    ExecutionFailure = 7,
}
