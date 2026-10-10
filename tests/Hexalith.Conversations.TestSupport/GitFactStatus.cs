namespace Hexalith.Conversations.TestSupport;

/// <summary>Classifies a Git fact without treating missing authority as empty success.</summary>
public enum GitFactStatus
{
    /// <summary>The requested fact was established.</summary>
    Success,
    /// <summary>Git or required history is unavailable.</summary>
    Unavailable,
    /// <summary>The request or Git output is invalid.</summary>
    Invalid,
    /// <summary>The process exceeded its execution limit.</summary>
    Timeout,
    /// <summary>The caller cancelled execution.</summary>
    Cancelled,
}
