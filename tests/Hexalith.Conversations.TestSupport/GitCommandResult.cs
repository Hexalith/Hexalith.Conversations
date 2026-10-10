namespace Hexalith.Conversations.TestSupport;

/// <summary>Complete raw output of a bounded Git command.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Stdout">Exact standard-output bytes.</param>
/// <param name="Stderr">Exact standard-error bytes.</param>
public sealed record GitCommandResult(int ExitCode, byte[] Stdout, byte[] Stderr);
