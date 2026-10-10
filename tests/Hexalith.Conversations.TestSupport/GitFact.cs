namespace Hexalith.Conversations.TestSupport;

/// <summary>A typed result for a Git-backed fact.</summary>
/// <typeparam name="T">The successful fact type.</typeparam>
/// <param name="Status">The outcome.</param>
/// <param name="Value">The fact, present only on success.</param>
/// <param name="Code">A stable failure code, or null on success.</param>
/// <param name="Message">A diagnostic suitable for a test failure.</param>
public sealed record GitFact<T>(GitFactStatus Status, T? Value, string? Code = null, string? Message = null)
{
    /// <summary>Whether a fact was established.</summary>
    public bool IsSuccess => Status == GitFactStatus.Success;

    /// <summary>Constructs a successful fact.</summary>
    public static GitFact<T> Ok(T value) => new(GitFactStatus.Success, value);

    /// <summary>Constructs a failure without a fact.</summary>
    public static GitFact<T> Fail(GitFactStatus status, string code, string message) => new(status, default, code, message);
}
