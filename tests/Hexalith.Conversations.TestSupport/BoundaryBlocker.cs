namespace Hexalith.Conversations.TestSupport;

/// <summary>A measured evidence-boundary violation.</summary>
/// <param name="Code">The contracted blocker code.</param>
/// <param name="Subject">The path or assertion that violated the boundary.</param>
/// <param name="Message">The measured failure detail.</param>
public sealed record BoundaryBlocker(string Code, string Subject, string Message);
